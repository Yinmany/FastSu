﻿namespace FastSu.GenTools.Base;

public static class SLog
{
    public static void Info(object msg)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(msg);
    }

    public static void Error(object msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
    }
}