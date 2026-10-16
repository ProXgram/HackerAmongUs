using System;
using System.Text;

namespace HAU;

public sealed class MenuSearchInput
{
    public const int MaximumLength = 80;
    public string Text { get; private set; } = "";
    public bool IsEditing { get; private set; }
    public bool AllSelected { get; private set; }

    public void BeginEditing()
    {
        IsEditing = true;
        AllSelected = false;
    }

    public void StopEditing()
    {
        IsEditing = false;
        AllSelected = false;
    }

    public void SelectAll()
    {
        if (IsEditing) AllSelected = true;
    }

    public void Clear()
    {
        Text = "";
        StopEditing();
    }

    public void Paste(string text)
    {
        if (!IsEditing) return;
        var filtered = new StringBuilder();
        foreach (char value in text ?? "")
            if (!char.IsControl(value) && value is not '\u2028' and not '\u2029') filtered.Append(value);
        Type(filtered.ToString());
    }

    public bool Type(string input)
    {
        if (!IsEditing) return false;
        var text = new StringBuilder(Text);
        bool submitted = false;
        for (int index = 0; index < input.Length; index++)
        {
            char value = input[index];
            if (value is '\r' or '\n')
            {
                submitted = true;
                StopEditing();
                break;
            }
            if (value == '\b')
            {
                if (AllSelected) text.Clear();
                else if (text.Length > 0)
                {
                    int count = text.Length >= 2 && char.IsLowSurrogate(text[text.Length - 1]) &&
                        char.IsHighSurrogate(text[text.Length - 2]) ? 2 : 1;
                    text.Length -= count;
                }
                AllSelected = false;
                continue;
            }
            if (char.IsControl(value) || char.IsLowSurrogate(value) || value is '\u2028' or '\u2029') continue;
            bool pair = char.IsHighSurrogate(value);
            if (pair && (index + 1 >= input.Length || !char.IsLowSurrogate(input[index + 1]))) continue;
            if (AllSelected) text.Clear();
            AllSelected = false;
            int length = pair ? 2 : 1;
            if (text.Length + length <= MaximumLength)
            {
                text.Append(value);
                if (pair) text.Append(input[index + 1]);
            }
            if (pair) index++;
        }
        Text = text.ToString();
        return submitted;
    }
}
