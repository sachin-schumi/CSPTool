using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


public class Constants {
    public static Dictionary<string, string> constantMap = new Dictionary<string, string>()
    {
        {"Agents",@"([\w\d]+):([\w\d]+)"},
        {"DataType",@"([\w\d]+):([\w\d]+)"},
        {"Process",@"([\w\d]+):([\w\d]+)[\s]knows[\s]([\w\d,]+)"},
        {"Keys",@""},
        {"KeysSym",@"([\w]+),([\w]+){([\w]+),([\w]+)}"},
        {"KeysAsym",@"([\w\d]+){([\w\d]+),([\w\d]+)}"},
        {"Message1",@"([\w\d]*)->([\w\d]*)"},
        {"Message2",@"([\w]Enc){([\w\d,\.]*)}"},
        {"Message3",@"([\w\d\.]+$)"},
        {"Protocol",""},
        {"Decisions",@"(([\w\d\.]*)(==|!=)([\w\d\.]*))"}
    };
}
