using System.Text;
using FastSu;

namespace FastSu.Tests;

public class GiftCodeTests
{
    [Test]
    public void Test()
    {
        GiftCodeGenerator generator = new GiftCodeGenerator(100000, 0);

        for (int i = 0; i < 25; i++)
        {
            Span<byte> buff = new byte[8];
            generator.Next(buff);
            string code = Encoding.ASCII.GetString(buff);
            bool b = generator.TryParse(code, out int id);
            Console.WriteLine(id + "|" + code + "|" + b);
        }
    }

    [Test]
    public void Test1()
    {
        GiftCodeGenerator generator = new GiftCodeGenerator(100000, 0);
        Span<byte> buff = new byte[8];
        generator.Next(buff);
        string code = Encoding.ASCII.GetString(buff);
        Console.WriteLine(code);

        if (generator.TryParse(code, out int id))
        {
            Console.WriteLine(id);
        }
        else
        {
            Console.WriteLine("解码失败!");
        }
    }

    [Test]
    public void Test2()
    {
        Console.WriteLine(1189 % 32);
    }
}