using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;

public class InputParser
{
    readonly string[] sectionSeparator = new string[] { "----\r\n" };
    readonly string[] subSectionSeparator = new string[] { "\r\n" };
    readonly string lineSeparator = @"\s+";
    public int errorlineNumber = 0;
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
        String[] alllines = fileText.Split(subSectionSeparator,StringSplitOptions.None);
        String[] section = fileText.Split(sectionSeparator, StringSplitOptions.None);
        bool isParseable = true;

        HashSet<String> headerNames = new HashSet<String>() { "Agents", "DataType", "Process", "Keys", "Protocol" };

        int[] errorLine = new int[2];
        int i = 0;
        try
        {
            for (; i < section.Length && isParseable == true; i++)
            {
                String[] parts = section[i].Split(subSectionSeparator, StringSplitOptions.None);
                String[] header = Regex.Split(parts[0], lineSeparator);
                String headerName = header[0].Substring(1);
                errorLine[0] = Array.IndexOf(alllines, parts[0]) + 1;                
                if( !(headerNames.Contains(headerName.Trim())))
                {
                    isParseable = false;
                    break;
                }                
                int n = Int32.Parse(header[1]);
                if (headerName == "Protocol")
                {
                    Protocol p = new Protocol();
                    isParseable = isParseable && p.parseHelper(n, parts, Constants.constantMap[headerName], messages, errorLine);
                }
                else
                {
                    object[] parameters = new object[4];
                    Type t = Type.GetType(headerName, true);
                    Object o = (Activator.CreateInstance(t));
                    parameters[0] = (int)n;
                    parameters[1] = (String[])parts;
                    parameters[2] = Constants.constantMap[headerName];
                    parameters[3] = errorLine;
                    MethodInfo mi = o.GetType().GetMethod("parse");
                    isParseable = isParseable && (bool)mi.Invoke(o, parameters);
                }
            }
        }
        catch (Exception e)
        {
            errorLine[1] = i;
            isParseable = false;
        }
        finally
        {
            if (isParseable == false)
            {
                errorlineNumber = errorLine[0] + errorLine[1];
            }
        }
        return isParseable;
    }
}
