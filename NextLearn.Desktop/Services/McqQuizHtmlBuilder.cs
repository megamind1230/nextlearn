using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextLearn.Desktop.Models;

namespace NextLearn.Desktop.Services;

public static class McqQuizHtmlBuilder
{
    private static readonly MarkdownInlineRenderer MarkdownRenderer = new();

    public static string BuildQuestionHtml(
        McqQuestion question,
        int index,
        int total,
        TimeSpan elapsed,
        bool showAnswer = false,
        string profile = "Vim")
    {
        ArgumentNullException.ThrowIfNull(question);

        var cardsHtml = BuildOptionCards(question, showAnswer);
        var timerDisplay = $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}";
        var showExplanation = showAnswer
            && question.SelectedIndex.HasValue
            && question.Explanation != null;

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<meta name=""color-scheme"" content=""dark"">
<style>
  * {{ margin: 0; padding: 0; box-sizing: border-box; }}
  html {{ background: #0F172A; }}
  html, body {{ min-height: 100%; }}
  body {{
    background: #0F172A;
    color: #E2E8F0;
    font-family: system-ui, -apple-system, sans-serif;
    padding: 24px;
    min-height: 100vh;
  }}
  .header {{
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 24px;
  }}
  .progress {{ font-size: 14px; color: #94A3B8; }}
  .question-text {{
    font-size: 18px;
    line-height: 1.6;
    margin-bottom: 32px;
    padding: 20px;
    background: #1E293B;
    border-radius: 12px;
    border: 1px solid #334155;
  }}
  .question-text p {{ margin-bottom: 8px; }}
  .question-text p:last-child {{ margin-bottom: 0; }}
  .grid {{
    display: flex;
    flex-direction: column;
    gap: 12px;
  }}
  .card {{
    background: #1E293B;
    border: 2px solid #334155;
    border-radius: 12px;
    padding: 16px 20px;
    cursor: pointer;
    transition: all 0.15s ease;
    display: flex;
    align-items: center;
    width: 100%;
  }}
  .card:hover {{ border-color: #2563EB; background: #1E3A5F; }}
  .card.selected {{
    border-color: #2563EB;
    background: #1E3A5F;
  }}
  .card.correct {{
    border-color: #10B981;
    background: #064E3B;
  }}
  .card.wrong {{
    border-color: #EF4444;
    background: #450A0A;
  }}
  .card-letter {{
    font-size: 24px;
    font-weight: bold;
    color: #2563EB;
    margin-right: 16px;
    flex-shrink: 0;
  }}
  .card-content {{
    font-size: 17px;
    line-height: 1.5;
    flex: 1;
    min-width: 0;
  }}
  .card-content p {{ margin-bottom: 4px; }}
  .card-content p:last-child {{ margin-bottom: 0; }}
  .card-content code {{
    background: #0F172A;
    padding: 2px 6px;
    border-radius: 4px;
    font-size: 13px;
  }}
  .card-content pre {{ margin: 8px 0; }}
  .card-content blockquote {{ margin: 8px 0; }}
  .card-content .math-display {{ margin: 8px 0; }}
  .explanation {{
    margin-top: 24px;
    padding: 16px;
    background: #1E293B;
    border-left: 4px solid #FBBF24;
    border-radius: 8px;
    font-size: 14px;
    line-height: 1.5;
    color: #94A3B8;
  }}
  code {{ font-family: 'JetBrains Mono', 'Fira Code', Consolas, monospace; font-size: 0.9em; background: #78350F; color: #FBBF24; padding: 2px 6px; border-radius: 4px; }}
  pre {{ background: #282C34; border-radius: 8px; padding: 16px; overflow-x: auto; margin: 0 0 12px 0; white-space: pre; position: relative; width: max-content; max-width: calc(100vw - 48px); }}
  pre code {{ background: none; color: #ABB2BF; padding: 0; border-radius: 0; font-size: 0.85em; }}
  blockquote {{ margin: 0 0 12px 0; padding: 8px 16px; border-left: 4px solid #60A5FA; background: #334155; border-radius: 0 4px 4px 0; position: relative; width: max-content; max-width: calc(100vw - 48px); }}
  blockquote p {{ margin: 0 0 4px 0; }}
  table {{ border-collapse: collapse; width: 100%; margin: 0 0 12px 0; }}
  th, td {{ border: 1px solid #475569; padding: 8px 12px; text-align: left; }}
  th {{ background: #334155; color: #F1F5F9; font-weight: 600; }}
  td {{ color: #E2E8F0; }}
  ul, ol {{ margin: 0 0 12px 0; padding-left: 24px; }}
  li {{ margin: 4px 0; }}
  a {{ color: #60A5FA; text-decoration: underline; }}
  a:hover {{ color: #93C5FD; }}
  strong {{ font-weight: 700; color: #F8FAFC; }}
  em {{ font-style: italic; color: #CBD5E1; }}
  .math-display {{ background: #282C34; border-radius: 8px; padding: 16px; margin: 0 0 12px 0; overflow-x: auto; position: relative; width: max-content; max-width: calc(100vw - 48px); }}
  .katex {{ cursor: copy; }}
  .katex-display .katex {{ cursor: auto; }}
  .katex-html {{ padding-right: 3em; }}
  .katex-display {{ position: relative; }}
  <!--HIGHLIGHT_CSS-->
  <!--KATEX_CSS-->
  .inline-copied {{ box-shadow: 0 0 0 2px #10B981; border-radius: 2px; transition: box-shadow 0.3s; }}
  .copy-btn {{
      position: absolute; top: 8px; right: 8px;
      background: #475569; color: #E2E8F0; border: none;
      border-radius: 4px; padding: 4px 8px; font-size: 12px;
      cursor: pointer; opacity: 0; line-height: 1;
      z-index: 10;
      transition: opacity 0.2s;
  }}
  blockquote .copy-btn {{ top: 4px; right: 4px; }}
  pre:hover .copy-btn, blockquote:hover .copy-btn, .math-display:hover .copy-btn {{ opacity: 1; }}
  .copy-btn:hover {{ background: #64748B; }}
  .copy-btn.copied {{ background: #10B981; }}
</style>
</head>
<body>
<div class=""header"">
  <span class=""progress"">Question {index + 1} of {total}</span>
</div>
<div class=""question-text"">{HtmlContentBuilder.RenderBlock(question.Question)}</div>
<div class=""grid"">
  {cardsHtml}
</div>
{(showExplanation ? $@"<div class=""explanation"">{HtmlContentBuilder.RenderBlock(question.Explanation ?? string.Empty)}</div>" : string.Empty)}
<script>/* HIGHLIGHT_JS */</script>
<script>hljs.highlightAll();</script>
<script>/* KATEX_AUTO_RENDER */</script>
<script>(function(){{function c(e,t){{var n=document.createElement('textarea');n.value=t.trim();n.style.cssText='position:fixed;opacity:0';document.body.appendChild(n);n.select();document.execCommand('copy');document.body.removeChild(n);e.classList.add('inline-copied');setTimeout(function(){{e.classList.remove('inline-copied')}},300)}}document.addEventListener('click',function(e){{var t;if(t=e.target.closest('code')){{if(!t.closest('pre')&&!t.closest('a')){{c(t,t.textContent);return}}}}if(t=e.target.closest('.katex')){{if(!t.closest('.katex-display')&&!t.closest('a')){{var a=t.querySelector('annotation');c(t,a?a.textContent:t.textContent)}}}}}})}})();</script>
<script>(function(){{function textWithout(el){{var clone=el.cloneNode(true);clone.querySelectorAll('.copy-btn').forEach(function(b){{b.remove()}});return clone.textContent}}var e=document.querySelectorAll('pre,blockquote,.math-display');for(var i=0;i<e.length;i++){{var p=e[i];var c=document.createElement('button');c.className='copy-btn';c.textContent='Copy';c.addEventListener('click',function(el,btn){{return function(){{var t=el.getAttribute('data-latex');if(!t){{t=textWithout(el)}}var ta=document.createElement('textarea');ta.value=t.trim();ta.style.position='fixed';ta.style.opacity='0';document.body.appendChild(ta);ta.select();document.execCommand('copy');document.body.removeChild(ta);btn.textContent='Copied!';btn.classList.add('copied');setTimeout(function(){{btn.textContent='Copy';btn.classList.remove('copied')}},2000)}}}}(p,c));p.appendChild(c)}}}})();</script>
<script>
  (function(){{
    var profile = '{profile}';
    var isVim = profile === 'Vim' || profile === 'VS Code';
    var isEmacs = profile === 'Emacs';
    var isVSCode = profile === 'VS Code';
    var chord = null, chordTimer = null;
    document.addEventListener('keydown', function(e) {{
      var key = e.key, upper = key.toUpperCase();
      // MCQ answers: A/B/C/D (bare keys only)
      if (!e.ctrlKey && !e.altKey && !e.metaKey && ['A','B','C','D'].includes(upper)) {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/' + upper;
        return;
      }}
      // Nav: N/P — Emacs: only when Ctrl held; others: always
      if (!e.altKey && !e.metaKey && (isEmacs ? e.ctrlKey : true) && upper === 'N') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/next';
        return;
      }}
      if (!e.altKey && !e.metaKey && (isEmacs ? e.ctrlKey : true) && upper === 'P') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/prev';
        return;
      }}
      // Vim/VS Code: j/k/h/l scroll, q/d quit (bare keys)
      if (isVim && !e.ctrlKey && !e.altKey && !e.metaKey) {{
        if (key === 'j') {{ e.preventDefault(); window.scrollBy(0, 50); return; }}
        if (key === 'k') {{ e.preventDefault(); window.scrollBy(0, -50); return; }}
        if (key === 'h') {{ e.preventDefault(); window.scrollBy(-50, 0); return; }}
        if (key === 'l') {{ e.preventDefault(); window.scrollBy(50, 0); return; }}
        if (key === 'q' || key === 'd') {{
          e.preventDefault(); e.stopPropagation();
          window.location = 'http://mcq-answer.local/QUIT';
          return;
        }}
      }}
      // VS Code only: Ctrl+W quit
      if (isVSCode && e.ctrlKey && !e.altKey && !e.metaKey && key === 'w') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/QUIT';
        return;
      }}
      // Emacs: C-f, C-b, C-v, M-v scroll
      if (isEmacs && e.ctrlKey && !e.altKey && !e.metaKey) {{
        if (key === 'f') {{ e.preventDefault(); window.scrollBy(50, 0); return; }}
        if (key === 'b') {{ e.preventDefault(); window.scrollBy(-50, 0); return; }}
        if (key === 'v') {{ e.preventDefault(); window.scrollBy(0, 50); return; }}
      }}
      if (isEmacs && e.altKey && !e.ctrlKey && !e.metaKey && key === 'v') {{
        e.preventDefault(); window.scrollBy(0, -50); return;
      }}
      // Emacs: C-x q / C-x d quit (chord)
      if (isEmacs && e.ctrlKey && !e.altKey && !e.shiftKey && key === 'x') {{
        e.preventDefault();
        chord = 'C-x';
        if (chordTimer) clearTimeout(chordTimer);
        chordTimer = setTimeout(function() {{ chord = null; chordTimer = null; }}, 500);
        return;
      }}
      if (isEmacs && chord === 'C-x' && (key === 'q' || key === 'd')) {{
        e.preventDefault(); clearTimeout(chordTimer);
        chord = null; chordTimer = null;
        window.location = 'http://mcq-answer.local/QUIT';
        return;
      }}
      // Common: Escape quit
      if (key === 'Escape') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/QUIT';
        return;
      }}
      // Alt+Left/Right: prevent browser back/forward, navigate question
      if (e.altKey && (key === 'ArrowLeft' || key === 'ArrowRight')) {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/' + (key === 'ArrowLeft' ? 'prev' : 'next');
        return;
      }}
      // Ctrl+G: quit quiz (Emacs keyboard quit)
      if (isEmacs && e.ctrlKey && !e.altKey && !e.metaKey && key === 'g') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/QUIT';
        return;
      }}
      // F1: prevent browser help, route through C# handler
      if (key === 'F1' || key === 'f1') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/F1';
        return;
      }}
      // Cancel any pending chord
      if (chord) {{ clearTimeout(chordTimer); chord = null; chordTimer = null; }}
    }}, true);
  }})();
</script>
</body>
</html>";
    }

    public static string BuildReviewHtml(List<McqQuestion> questions, TimeSpan elapsed, string profile = "Vim")
    {
        ArgumentNullException.ThrowIfNull(questions);
        var total = questions.Count;
        var answered = questions.Count(q => q.IsAnswered);
        var correct = questions.Count(q => q.IsCorrect);
        var scorePercent = total > 0 ? (int)(correct * 100.0 / total) : 0;
        var timerDisplay = $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}";

        var questionsHtml = new StringBuilder();
        foreach (var q in questions)
        {
            var correctLetter = ((char)('A' + q.CorrectIndex)).ToString();
            var selectedLetter = q.SelectedIndex.HasValue ? ((char)('A' + q.SelectedIndex.Value)).ToString() : "—";
            var status = q.IsAnswered ? (q.IsCorrect ? "✅" : "❌") : "⏭️";
            var statusClass = q.IsAnswered ? (q.IsCorrect ? "correct" : "wrong") : "skipped";

            questionsHtml.AppendLine($@"
<div class=""review-question"">
  <div class=""review-header {statusClass}"">
    <span class=""review-status"">{status}</span>
    <div class=""review-question-text"">{HtmlContentBuilder.RenderBlock(q.Question)}</div>
  </div>
  <div class=""review-options"">");

            for (var i = 0; i < q.Options.Count; i++)
            {
                var letter = ((char)('A' + i)).ToString();
                var isSelected = q.SelectedIndex == i;
                var isCorrect = q.CorrectIndex == i;
                var optionClass = isCorrect ? "option-correct" : (isSelected && !isCorrect ? "option-wrong" : string.Empty);

                questionsHtml.AppendLine($@"
    <div class=""option {optionClass}"">
      <span class=""option-letter"">{letter}.</span>
      <span class=""option-text"">{MarkdownRenderer.RenderInline(q.Options[i])}</span>
      {(isCorrect ? @"<span class=""option-mark"">✓</span>" : string.Empty)}
      {(isSelected && !isCorrect ? $@"<span class=""option-mark"">✗ (you picked {selectedLetter})</span>" : string.Empty)}
    </div>");
            }

            if (!string.IsNullOrEmpty(q.Explanation))
            {
                questionsHtml.AppendLine($@"
  <div class=""review-explanation"">
    <strong>Explanation:</strong> {HtmlContentBuilder.RenderBlock(q.Explanation)}
  </div>");
            }

            questionsHtml.AppendLine("</div></div>");
        }

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<meta name=""color-scheme"" content=""dark"">
<style>
  * {{ margin: 0; padding: 0; box-sizing: border-box; }}
  html {{ background: #0F172A; }}
  body {{
    background: #0F172A;
    color: #E2E8F0;
    font-family: system-ui, -apple-system, sans-serif;
    padding: 24px;
  }}
  .summary {{
    text-align: center;
    margin-bottom: 32px;
    padding: 24px;
    background: #1E293B;
    border-radius: 12px;
    border: 1px solid #334155;
  }}
  .score {{ font-size: 48px; font-weight: bold; }}
  .score.good {{ color: #10B981; }}
  .score.ok {{ color: #FBBF24; }}
  .score.bad {{ color: #EF4444; }}
  .stats {{ margin-top: 12px; font-size: 14px; color: #94A3B8; }}
  .stats span {{ margin: 0 8px; }}
  .review-question {{
    margin-bottom: 20px;
    padding: 16px;
    background: #1E293B;
    border-radius: 12px;
    border: 1px solid #334155;
  }}
  .review-header {{
    display: flex;
    align-items: flex-start;
    gap: 12px;
    margin-bottom: 12px;
  }}
  .review-status {{ font-size: 20px; flex-shrink: 0; }}
  .review-question-text {{ font-size: 16px; font-weight: 600; line-height: 1.5; }}
  .review-options {{ margin-left: 36px; }}
  .option {{
    padding: 8px 12px;
    margin-bottom: 4px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 14px;
    line-height: 1.4;
  }}
  .option-correct {{ background: #064E3B; }}
  .option-wrong {{ background: #450A0A; }}
  .option-letter {{ font-weight: bold; color: #94A3B8; flex-shrink: 0; }}
  .option-text {{ flex: 1; }}
  .option-text code {{
    background: #0F172A;
    padding: 1px 4px;
    border-radius: 4px;
    font-size: 13px;
  }}
  .option-mark {{ font-size: 12px; color: #94A3B8; flex-shrink: 0; }}
  .review-explanation {{
    margin-top: 12px;
    margin-left: 36px;
    padding: 12px;
    background: #0F172A;
    border-left: 3px solid #FBBF24;
    border-radius: 6px;
    font-size: 13px;
    line-height: 1.5;
    color: #94A3B8;
  }}
  .review-header.correct {{ border-left: 3px solid #10B981; padding-left: 8px; }}
  .review-header.wrong {{ border-left: 3px solid #EF4444; padding-left: 8px; }}
  .review-header.skipped {{ border-left: 3px solid #64748B; padding-left: 8px; }}
  code {{ font-family: 'JetBrains Mono', 'Fira Code', Consolas, monospace; font-size: 0.9em; background: #78350F; color: #FBBF24; padding: 2px 6px; border-radius: 4px; }}
  pre {{ background: #282C34; border-radius: 8px; padding: 16px; overflow-x: auto; margin: 0 0 12px 0; white-space: pre; position: relative; width: max-content; max-width: calc(100vw - 48px); }}
  pre code {{ background: none; color: #ABB2BF; padding: 0; border-radius: 0; font-size: 0.85em; }}
  blockquote {{ margin: 0 0 12px 0; padding: 8px 16px; border-left: 4px solid #60A5FA; background: #334155; border-radius: 0 4px 4px 0; position: relative; width: max-content; max-width: calc(100vw - 48px); }}
  blockquote p {{ margin: 0 0 4px 0; }}
  table {{ border-collapse: collapse; width: 100%; margin: 0 0 12px 0; }}
  th, td {{ border: 1px solid #475569; padding: 8px 12px; text-align: left; }}
  th {{ background: #334155; color: #F1F5F9; font-weight: 600; }}
  td {{ color: #E2E8F0; }}
  ul, ol {{ margin: 0 0 12px 0; padding-left: 24px; }}
  li {{ margin: 4px 0; }}
  a {{ color: #60A5FA; text-decoration: underline; }}
  a:hover {{ color: #93C5FD; }}
  strong {{ font-weight: 700; color: #F8FAFC; }}
  em {{ font-style: italic; color: #CBD5E1; }}
  .math-display {{ background: #282C34; border-radius: 8px; padding: 16px; margin: 0 0 12px 0; overflow-x: auto; position: relative; width: max-content; max-width: calc(100vw - 48px); }}
  .katex {{ cursor: copy; }}
  .katex-display .katex {{ cursor: auto; }}
  .katex-html {{ padding-right: 3em; }}
  .katex-display {{ position: relative; }}
  <!--HIGHLIGHT_CSS-->
  <!--KATEX_CSS-->
  .inline-copied {{ box-shadow: 0 0 0 2px #10B981; border-radius: 2px; transition: box-shadow 0.3s; }}
  .copy-btn {{
      position: absolute; top: 8px; right: 8px;
      background: #475569; color: #E2E8F0; border: none;
      border-radius: 4px; padding: 4px 8px; font-size: 12px;
      cursor: pointer; opacity: 0; line-height: 1;
      z-index: 10;
      transition: opacity 0.2s;
  }}
  blockquote .copy-btn {{ top: 4px; right: 4px; }}
  pre:hover .copy-btn, blockquote:hover .copy-btn, .math-display:hover .copy-btn {{ opacity: 1; }}
  .copy-btn:hover {{ background: #64748B; }}
  .copy-btn.copied {{ background: #10B981; }}
</style>
</head>
<body>
<div class=""summary"">
  <div class=""score {(scorePercent >= 80 ? "good" : scorePercent >= 50 ? "ok" : "bad")}"">{scorePercent}%</div>
  <div class=""stats"">
    <span>{correct}/{total} correct</span>
    <span>|</span>
    <span>{answered} answered, {total - answered} skipped</span>
    <span>|</span>
    <span>Time: {timerDisplay}</span>
  </div>
</div>
{questionsHtml}
<script>/* HIGHLIGHT_JS */</script>
<script>hljs.highlightAll();</script>
<script>/* KATEX_AUTO_RENDER */</script>
<script>(function(){{function c(e,t){{var n=document.createElement('textarea');n.value=t.trim();n.style.cssText='position:fixed;opacity:0';document.body.appendChild(n);n.select();document.execCommand('copy');document.body.removeChild(n);e.classList.add('inline-copied');setTimeout(function(){{e.classList.remove('inline-copied')}},300)}}document.addEventListener('click',function(e){{var t;if(t=e.target.closest('code')){{if(!t.closest('pre')&&!t.closest('a')){{c(t,t.textContent);return}}}}if(t=e.target.closest('.katex')){{if(!t.closest('.katex-display')&&!t.closest('a')){{var a=t.querySelector('annotation');c(t,a?a.textContent:t.textContent)}}}}}})}})();</script>
<script>(function(){{function textWithout(el){{var clone=el.cloneNode(true);clone.querySelectorAll('.copy-btn').forEach(function(b){{b.remove()}});return clone.textContent}}var e=document.querySelectorAll('pre,blockquote,.math-display');for(var i=0;i<e.length;i++){{var p=e[i];var c=document.createElement('button');c.className='copy-btn';c.textContent='Copy';c.addEventListener('click',function(el,btn){{return function(){{var t=el.getAttribute('data-latex');if(!t){{t=textWithout(el)}}var ta=document.createElement('textarea');ta.value=t.trim();ta.style.position='fixed';ta.style.opacity='0';document.body.appendChild(ta);ta.select();document.execCommand('copy');document.body.removeChild(ta);btn.textContent='Copied!';btn.classList.add('copied');setTimeout(function(){{btn.textContent='Copy';btn.classList.remove('copied')}},2000)}}}}(p,c));p.appendChild(c)}}}})();</script>
<script>
  (function(){{
    var profile = '{profile}';
    var isVim = profile === 'Vim' || profile === 'VS Code';
    var isEmacs = profile === 'Emacs';
    var isVSCode = profile === 'VS Code';
    var chord = null, chordTimer = null;
    document.addEventListener('keydown', function(e) {{
      var key = e.key;
      // Vim/VS Code: j/k/h/l scroll, q/d quit (bare keys)
      if (isVim && !e.ctrlKey && !e.altKey && !e.metaKey) {{
        if (key === 'j') {{ e.preventDefault(); window.scrollBy(0, 50); return; }}
        if (key === 'k') {{ e.preventDefault(); window.scrollBy(0, -50); return; }}
        if (key === 'h') {{ e.preventDefault(); window.scrollBy(-50, 0); return; }}
        if (key === 'l') {{ e.preventDefault(); window.scrollBy(50, 0); return; }}
        if (key === 'q' || key === 'd') {{
          e.preventDefault(); e.stopPropagation();
          window.location = 'http://mcq-answer.local/QUIT';
          return;
        }}
      }}
      // VS Code only: Ctrl+W quit
      if (isVSCode && e.ctrlKey && !e.altKey && !e.metaKey && key === 'w') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/QUIT';
        return;
      }}
      // Emacs: C-f, C-b, C-v, M-v scroll
      if (isEmacs && e.ctrlKey && !e.altKey && !e.metaKey) {{
        if (key === 'f') {{ e.preventDefault(); window.scrollBy(50, 0); return; }}
        if (key === 'b') {{ e.preventDefault(); window.scrollBy(-50, 0); return; }}
        if (key === 'v') {{ e.preventDefault(); window.scrollBy(0, 50); return; }}
      }}
      if (isEmacs && e.altKey && !e.ctrlKey && !e.metaKey && key === 'v') {{
        e.preventDefault(); window.scrollBy(0, -50); return;
      }}
      // Emacs: C-x q / C-x d quit (chord)
      if (isEmacs && e.ctrlKey && !e.altKey && !e.shiftKey && key === 'x') {{
        e.preventDefault();
        chord = 'C-x';
        if (chordTimer) clearTimeout(chordTimer);
        chordTimer = setTimeout(function() {{ chord = null; chordTimer = null; }}, 500);
        return;
      }}
      if (isEmacs && chord === 'C-x' && (key === 'q' || key === 'd')) {{
        e.preventDefault(); clearTimeout(chordTimer);
        chord = null; chordTimer = null;
        window.location = 'http://mcq-answer.local/QUIT';
        return;
      }}
      // Common: Escape quit
      if (key === 'Escape') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/QUIT';
        return;
      }}
      // Alt+Left/Right: prevent browser back/forward navigation
      if (e.altKey && (key === 'ArrowLeft' || key === 'ArrowRight')) {{
        e.preventDefault(); e.stopPropagation();
        return;
      }}
      // Ctrl+G: quit quiz (Emacs keyboard quit)
      if (isEmacs && e.ctrlKey && !e.altKey && !e.metaKey && key === 'g') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/QUIT';
        return;
      }}
      // F1: prevent browser help, route through C# handler
      if (key === 'F1' || key === 'f1') {{
        e.preventDefault(); e.stopPropagation();
        window.location = 'http://mcq-answer.local/F1';
        return;
      }}
      // Cancel any pending chord
      if (chord) {{ clearTimeout(chordTimer); chord = null; chordTimer = null; }}
    }}, true);
  }})();
</script>
</body>
</html>";
    }

    private static string BuildOptionCards(McqQuestion question, bool showAnswer)
    {
        var letters = new[] { "A", "B", "C", "D" };
        var cards = new StringBuilder();

        for (var i = 0; i < question.Options.Count; i++)
        {
            var letter = letters[i];
            var isSelected = question.SelectedIndex == i;

            string cssClass;
            if (showAnswer && question.SelectedIndex.HasValue)
            {
                var isCorrect = question.CorrectIndex == i;
                cssClass = isCorrect ? "correct" : (isSelected ? "wrong" : string.Empty);
            }
            else
            {
                cssClass = isSelected ? "selected" : string.Empty;
            }

            cards.AppendLine($@"
  <div class=""card {cssClass}"" data-letter=""{letter}"" onclick=""window.location='http://mcq-answer.local/{letter}'"">
    <span class=""card-letter"">{letter}</span>
    <div class=""card-content"">{HtmlContentBuilder.RenderBlock(question.Options[i])}</div>
  </div>");
        }

        return cards.ToString();
    }
}
