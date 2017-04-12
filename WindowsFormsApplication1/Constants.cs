using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


public class Constants {
    public static Dictionary<string, string> constantMap = new Dictionary<string, string>()
    {
        {"Agents",@"([\w]+)[\s]+:[\s]+([\w]+)"},
        {"DataType",@"([\w]+[\*]*)[\s]+:[\s]+([\w]+)"},
        {"Process",@"([\w]+)[\s]*:[\s]*([\w]+)[\s]knows[\s]([\w\d,]+)"},
        {"Keys",@""},
        {"KeysSym",@"([\w]+),([\w]+){([\w]+),([\w]+)}"},
        {"KeysAsym",@"([\w]+){([\w]+),([\w]+)}"},
        {"Message1",@"([\w])[\s]*->[\s]*([\w])"},
        {"Message2",@"([\w]Enc){([\w\d,\.]*)}"},
        {"Message3",@"([\w\d\.]+)"},
        {"Protocol",""}
    };
}
