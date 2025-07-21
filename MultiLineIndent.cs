using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace VoteBot;

public class MultiLineIndentFormatter : ITextFormatter {

    public void Format(LogEvent logEvent, TextWriter output) {
        
        // 1. Format timestamp
        string timestamp = logEvent.Timestamp.ToString("HH:mm:ss");

        // 2. Get full level name
        string level = logEvent.Level.ToString();

        // 3. Write the header: [timestamp level]
        output.Write($"[{timestamp} {level}] ");
        
        // 4. Render message to a string
        using StringWriter msgWriter = new StringWriter();
        logEvent.RenderMessage(msgWriter);
        string message = msgWriter.ToString();
        
        // 5. Write message (first line inline, others indented)
        string[] messageLines = message.Replace("\r", "").Split('\n');
        if (messageLines.Length > 0) {
            // display the first line inline with the [time/level] info.
            output.WriteLine(messageLines[0]);
            
            // remove the first line as we have already displayed it.
            messageLines = messageLines.Skip(1).ToArray();
            
            // display the other lines.
            foreach (string line in messageLines) {
                output.WriteLine('\t' + line);
            }
        }

        // 6. Render and write exception, if any
        if (logEvent.Exception != null) {
            string[] exceptionLines = logEvent.Exception.ToString().Replace("\r", "").Split('\n');
            if (exceptionLines.Length > 0) {
                output.WriteLine('\t' + exceptionLines[0]);
                for (int i = 1; i < exceptionLines.Length; i++) {
                    output.WriteLine("\t\t" + exceptionLines[i]);
                }
            }
        }
    }
    
    private static void WriteIndentedLines(TextWriter output, string text, int indentLevel) {
        string indent = new string('\t', indentLevel);
        string[] lines = text.Replace("\r", "").Split('\n');
        for (int i = 0; i < lines.Length; i++) {
            if (i > 0) output.Write(Environment.NewLine);
            output.Write(indent + lines[i]);
        }
    }
    
    public static string WrapTextToTerminalWidth(string text) {
        int width;
        try {
            width = Console.WindowWidth;
        } catch {
            width = 80; // fallback if Console.WindowWidth not available
        }
        
        if (width <= 0) width = 80; // fallback width
        
        // break the string into words
        List<string> lines = new List<string>();
        string[] words = text.Split(' ');
        string currentLine = "";
        
        //TODO: we need to loop by intended line so when we split intended lines we can indent.
        
        // add all the words
        foreach (string word in words) {
            
            // if we can't add the current word to the current line,
            // save the line and start again.
            if ((currentLine + word).Length + 1 > width) {
                // if the line has any length, we are good and can continue.
                if (currentLine.Length > 0) {
                    lines.Add(currentLine.TrimEnd());
                    currentLine = "";
                }

                // If the currentLine.Length + word.Length is greater than width, 
                // and the currentLine is empty, then the word must be longer than
                // width. We will need to forcefully split it.
                else {
                    int index = 0;
                    while (index < word.Length) {
                        
                        // chunkSize is ether width or the rest of the word
                        int chunkSize = Math.Min(width, word.Length - index);
                        string chunk = word.Substring(index, chunkSize);

                        if (chunkSize < width) {
                            currentLine += chunk;
                        } else {
                            lines.Add(chunk);
                        }

                        index += chunkSize;
                    }
                }
            } else {
                currentLine += word + " ";
            }
        }

        if (currentLine.Length > 0) {
            lines.Add(currentLine.TrimEnd());
        }

        return string.Join("\n", lines);
    }

}
