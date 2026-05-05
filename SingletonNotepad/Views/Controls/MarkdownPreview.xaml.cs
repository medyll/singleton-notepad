using Markdig;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

namespace SingletonNotepad.Views.Controls;

public sealed partial class MarkdownPreview : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(MarkdownPreview),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public MarkdownPreview()
    {
        InitializeComponent();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownPreview preview && e.NewValue is string text)
            preview.RenderMarkdown(text);
    }

    private void RenderMarkdown(string markdown)
    {
        InnerRichTextBlock.Blocks.Clear();

        if (string.IsNullOrWhiteSpace(markdown))
            return;

        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var doc = Markdown.Parse(markdown, pipeline);

        foreach (var block in doc)
        {
            var para = new Paragraph();
            AppendBlock(para, block);
            if (para.Inlines.Count > 0)
                InnerRichTextBlock.Blocks.Add(para);
        }
    }

    private void AppendBlock(Paragraph para, Markdig.Syntax.Block block)
    {
        switch (block)
        {
            case Markdig.Syntax.HeadingBlock heading:
                para.Inlines.Add(new Run
                {
                    Text = (heading.Inline?.FirstChild?.ToString() ?? string.Empty) + "\n",
                    FontWeight = heading.Level == 1 ? FontWeights.Bold : FontWeights.SemiBold,
                    FontSize = heading.Level == 1 ? 24 : heading.Level == 2 ? 20 : 16,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 45, 90, 160)),
                });
                break;

            case Markdig.Syntax.ParagraphBlock paragraph:
                if (paragraph.Inline != null)
                    foreach (var inline in paragraph.Inline)
                        AppendInline(para, inline);
                para.Inlines.Add(new LineBreak());
                break;

            case Markdig.Syntax.ListBlock list:
                foreach (var item in list)
                {
                    if (item is not Markdig.Syntax.ListItemBlock listItem) continue;
                    para.Inlines.Add(new Run
                    {
                        Text = "• ",
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),
                    });
                    foreach (var child in listItem)
                    {
                        if (child is Markdig.Syntax.ParagraphBlock pb && pb.Inline != null)
                            foreach (var inline in pb.Inline)
                                AppendInline(para, inline);
                    }
                    para.Inlines.Add(new LineBreak());
                }
                break;

            case Markdig.Syntax.FencedCodeBlock fenced:
                para.Inlines.Add(new Run
                {
                    Text = string.Join("\n", fenced.Lines) + "\n",
                    FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                });
                break;

            case Markdig.Syntax.CodeBlock code:
                para.Inlines.Add(new Run
                {
                    Text = string.Join("\n", code.Lines) + "\n",
                    FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                });
                break;
        }
    }

    private static void AppendInline(Paragraph para, Markdig.Syntax.Inlines.Inline inline)
    {
        switch (inline)
        {
            case Markdig.Syntax.Inlines.LiteralInline literal:
                para.Inlines.Add(new Run { Text = literal.Content.ToString() });
                break;

            case Markdig.Syntax.Inlines.CodeInline code:
                para.Inlines.Add(new Run
                {
                    Text = code.Content,
                    FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                    FontStyle = FontStyle.Italic,
                });
                break;

            case Markdig.Syntax.Inlines.EmphasisInline emphasis:
                foreach (var child in emphasis)
                    AppendInline(para, child);
                break;

            case Markdig.Syntax.Inlines.LinkInline link:
                para.Inlines.Add(new Run
                {
                    Text = link.Title ?? link.FirstChild?.ToString() ?? string.Empty,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 50, 200)),
                });
                break;

            case Markdig.Syntax.Inlines.LineBreakInline:
                para.Inlines.Add(new LineBreak());
                break;

            case Markdig.Syntax.Inlines.HtmlInline html:
                para.Inlines.Add(new Run { Text = html.Tag });
                break;
        }
    }
}
