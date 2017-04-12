using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;

public class InputParser
{
    readonly string[] sectionSeparator = new string[] { "====\r\n" };
    readonly string[] subSectionSeparator = new string[] { "\r\n" };
    readonly string lineSeparator = @"\s+";

    public void clear()
    {
        Agents.agentMap.Clear();
        Process.aliases.Clear();
        Process.processMap.Clear();
        DataType.typeSet.Clear();
        Keys.keyMap.Clear();
        if (File.Exists(@"C:\FSDT\file.csp"))
            File.Delete(@"C:\FSDT\file.csp");
    }

    public bool ParseFile(String fileText,List<Message> messages)
    {
        clear();
        String[] section = fileText.Split(sectionSeparator, StringSplitOptions.None);
        bool isParseable = true;
        for (int i = 0; i < section.Length && isParseable == true ; i++)
        {
            String[] parts = section[i].Split(subSectionSeparator, StringSplitOptions.None);
            String[] header = Regex.Split(parts[0], lineSeparator);
            String headerName = header[0].Substring(1);
            int n = Int32.Parse(header[1]);
            if (headerName == "Protocol")
            {
                Protocol p = new Protocol();
                p.parseHelper(n, parts, Constants.constantMap[headerName], messages);
            }
            else
            {
                object[] parameters = new object[3];
                Type t = Type.GetType(headerName, true);
                Object o = (Activator.CreateInstance(t));
                parameters[0] = (int)n;
                parameters[1] = (String[])parts;
                parameters[2] = Constants.constantMap[headerName];
                MethodInfo mi = o.GetType().GetMethod("parse");
                isParseable = isParseable && (bool)mi.Invoke(o, parameters);
            }
        }
        return isParseable;
    }

    public String getError()
    {
        String error = "";
        return error;
    }
}
