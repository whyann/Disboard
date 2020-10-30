using Disboard;
using Gapos;

class Program
{
    [System.STAThread()]
    static void Main()
    {
        var disboard = new Disboard<GaposFactory>();
        disboard.Run(Token.token);
    }
}