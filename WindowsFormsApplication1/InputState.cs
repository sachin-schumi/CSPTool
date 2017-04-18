using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;


class DataType
{
    public static Dictionary<String, String> typeSet = new Dictionary<string, string>();
    public bool parse(int n, String[] lines, String regex, int[] errorLine)
    {
        HashSet<String> existingDataTypes = new HashSet<String>();
        int cnt = 1;
        for (int i = 1; i <= n; i++)
        {
            var match = Regex.Match(lines[i], regex);
            if (match.Success)
            {
                String message = match.Groups[1].Value;
                String type = match.Groups[2].Value;
                if (!(existingDataTypes.Contains(type)))
                {
                    typeSet[message] = type;
                    existingDataTypes.Add(type);
                }
                else
                {
                    typeSet[message] = type + cnt;
                    cnt++;
                }
            }
            else
            {
                errorLine[1] = i;
                return false;
            }
        }
        return true;
    }
}

class Process
{
    HashSet<string> agents = new HashSet<string>(Agents.agentMap.Values);
    HashSet<string> datatypes = new HashSet<string>(DataType.typeSet.Keys);
    public static Dictionary<String, String> aliases = new Dictionary<string, string>();


    public static Dictionary<String, String[]> processMap = new Dictionary<String, String[]>();
    public static Dictionary<String, String> processName = new Dictionary<String, String>();
    public static Dictionary<String, String[]> protocol = new Dictionary<String, String[]>();
    public bool parse(int n, String[] parts, String regex, int[] errorLine)
    {
        bool found = true;
        for (int i = 1; i <= n; i++)
        {
            var match = Regex.Match(parts[i], regex,RegexOptions.IgnoreCase);
            if (match.Success)
            {
                String procname = match.Groups[1].Value;
                String agent = match.Groups[2].Value;
                if (agents.Contains(agent))
                {
                    processName[agent] = procname;
                    String[] knownData = match.Groups[3].Value.Split(',');
                    for (int j = 0; j < knownData.Length; j++)
                    {
                        if (!datatypes.Contains(knownData[j]))
                        {
                            errorLine[1] = i;
                            found = false;
                            break;
                        }
                        aliases[agent + " " + knownData[j]] = DataType.typeSet[knownData[j]];
                    }
                    if (false == found)
                        break;
                    else
                    {
                        protocol[procname] = knownData;
                        processMap[agent] = knownData;
                    }
                }
                else
                {
                    errorLine[1] = i;
                    found = false;
                    break;
                }
            }
            else
            {
                errorLine[1] = i;
                found = false;
            }
        }
        return found;
    }
}

class Keys
{
    public static Dictionary<String, String[]> keyMap = new Dictionary<String, String[]>();
    public static Dictionary<String, String> inversekeys = new Dictionary<String, String>();
    public static Dictionary<String, String> encdec = new Dictionary<String, String> { 
        {"AEnc","ADec"},
        {"SEnc","SDec"}
    };

    public static HashSet<String> listofKeys = new HashSet<String>();
   
    public static String encryption = "";
    public static String decryption = "";
    public bool parse(int n, String[] lines, String regex,int[] errorLineNumber)
    {
        bool isParseable = false;
        if (n == 0)
            isParseable = true;
        else if (n == 1)
        {
            Match match = Regex.Match(lines[1], Constants.constantMap["KeysSym"]);
            String[] keys = new String[2];
            Keys.encryption = "SEnc";
            if (match.Success)
            {
                isParseable = true;
                String agent1 = match.Groups[1].Value;
                String agent2 = match.Groups[2].Value;
                keys[0] = match.Groups[3].Value;
                keys[1] = match.Groups[4].Value;
                listofKeys.Add(keys[0]);
                listofKeys.Add(keys[1]);
                keyMap[agent1] = keys;
                keyMap[agent2] = keys;
                inversekeys[keys[0]] = keys[1];
            }
            else
            {
                errorLineNumber[1] = 1;
            }
        }
        else if (n == 2)
        {
            Keys.encryption = "AEnc";
            for (int i = 1; i <= n; i++)
            {
                Match match = Regex.Match(lines[i], Constants.constantMap["KeysAsym"]);
                String[] keys = new String[2];
                if (match.Success)
                {
                    isParseable = true;
                    String agent = match.Groups[1].Value;
                    keys[0] = match.Groups[2].Value;
                    keys[1] = match.Groups[3].Value;
                    listofKeys.Add(keys[0]);
                    listofKeys.Add(keys[1]);
                    keyMap[agent] = keys;
                    inversekeys[keys[0]] = keys[1];
                }
                else
                {
                    errorLineNumber[1] = i;
                    isParseable = false;
                    break;
                }
            }
        }
        else
        {
            isParseable = false;
            errorLineNumber[1] = 0;
        }
        return isParseable;
    }
}

class Agents
{
    public static Dictionary<string, string> agentMap = new Dictionary<string, string>();
    public bool parse(int n, String[] lines, String regex, int[] errorLine)
    {
        for (int i = 1; i <= n; i++)
        {
            Match match = Regex.Match(lines[i], regex);
            if (match.Success)
            {
                agentMap[match.Groups[1].Value] = match.Groups[2].Value;
            }
            else
            {
                errorLine[1] = i;
                return false;
            }
        }
        return true;
    }
}


public class Message
{
    private String[] participators;
    private String[] keys;
    private List<String[]> decisions;
    private String decisionOrigin;

    public String DecisionOrigin
    {
        get { return decisionOrigin; }
        set { decisionOrigin = value; }
    }

    public List<String[]> Decisions
    {
        get { return decisions; }
        set { decisions = value; }
    }

    private String comparison;

    public String Comparison
    {
        get { return comparison; }
        set { comparison = value; }
    }


    public String[] Keys
    {
        get { return keys; }
        set { keys = value; }
    }
    public String[] Participators
    {
        get { return participators; }
        set { participators = value; }
    }
    private List<String> messageComponents;

    public List<String> MessageComponents
    {
        get { return messageComponents; }
        set { messageComponents = value; }
    }

    public String channel = "ca";
    private String inputChannel;

    public String InputChannel
    {
        get { return inputChannel; }
        set { inputChannel = value; }
    }
    private String outputChannel;

    public String OutputChannel
    {
        get { return outputChannel; }
        set { outputChannel = value; }
    }

    public Message()
    {
    }
}

class Protocol
{
    readonly string[] lineSeparator = new string[] { "|" };
    Dictionary<string, string> agents = new Dictionary<string, string>(Agents.agentMap);
    bool isParseable = true;

    public bool parseMessage(String str, Message m)
    {
        Match match = Regex.Match(str, Constants.constantMap["Message2"], RegexOptions.IgnoreCase);
        String message = "";
        if (match.Success)
        {
            String temp = match.Groups[2].Value;
            if (temp.IndexOf(",") > 0)
            {
                String[] key = new String[2];
                if (Keys.encryption != match.Groups[1].Value)
                    return false;
                Keys.decryption = Keys.encdec[Keys.encryption];
                message = temp.Substring(0, temp.IndexOf(","));
                key[0] = temp.Substring(temp.IndexOf(",") + 1);
                if (!Keys.listofKeys.Contains(key[0]))
                {
                    return false;
                }
                key[1] = Keys.inversekeys[key[0]];
                m.Keys = key;
            }
            else
            {
                isParseable = false;
                return isParseable;
            }
        }
        else
        {
            match = Regex.Match(str, Constants.constantMap["Message3"]);
            if (match.Success)
                message = match.Groups[1].Value;
            else
            {
                isParseable = false;
                return isParseable;
            }
        }
        List<String> list = new List<String>();
        if (message.IndexOf(".") > 0)
        {
            String[] components = message.Split('.');
            list = new List<string>(components);
        }
        else
            list.Add(message);

        for (int i = 0; i < list.Count; i++)
        {
            if (!DataType.typeSet.ContainsKey(list[i]))
                return false;
        }
        m.MessageComponents = list;
        return isParseable;
    }

    public bool parseProduction(String str, Message m)
    {
        Match match = Regex.Match(str, Constants.constantMap["Message1"]);
        if (!match.Success)
        {
            return false;
        }
        String[] participators = new String[2];
        participators[0] = match.Groups[1].Value;
        participators[1] = match.Groups[2].Value;
        if (!(agents.ContainsValue(participators[0]) && agents.ContainsValue(participators[1])))
            return false;
        m.Participators = participators;
        m.InputChannel = "!" + DataType.typeSet[participators[0]];
        m.OutputChannel = "?" + DataType.typeSet[participators[1]];
        return true;
    }


    public bool parseDecision(String str, Message m)
    {
        bool isParseable = true;
        try
        {
            String[] parts = str.Split(':');
            m.DecisionOrigin = parts[0];
            string[] andSeperator = new string[] { "&&" };
            List<String[]> listOfDecisions = new List<String[]>();
            if (str.IndexOf("&&") > 0)
            {
                String[] components = parts[1].Split(andSeperator, StringSplitOptions.None);
                for (int i = 0; i < components.Length; i++)
                {
                    Match match = Regex.Match(components[i], Constants.constantMap["Decisions"]);
                    if (!match.Success)
                    {
                        isParseable = false;
                        break;
                    }
                    String[] sides = new String[2];
                    sides[0] = match.Groups[2].Value;                    
                    sides[1] = match.Groups[4].Value;
                    m.Comparison = match.Groups[3].Value;
                    if (!(Process.aliases.ContainsKey(m.DecisionOrigin + " " + sides[0]) || Process.processMap.ContainsKey(sides[0])))
                    {
                        isParseable = false;
                        break;
                    }
                    listOfDecisions.Add(sides);
                }
            }
            else
            {
                Match match = Regex.Match(parts[1], Constants.constantMap["Decisions"]);
                if (!match.Success)
                {
                    isParseable = false;
                }
                String[] sides = new String[2];
                sides[0] = match.Groups[2].Value;
                sides[1] = match.Groups[4].Value;
                m.Comparison = match.Groups[3].Value;
                if (!(Process.aliases.ContainsKey(m.DecisionOrigin + " " + sides[0]) || Process.processMap.ContainsKey(sides[0])))
                {
                    isParseable = false;
                }
                listOfDecisions.Add(sides);
            }
            m.Decisions = listOfDecisions;
        }
        catch (Exception e)
        {
            isParseable = false;
        }
        return isParseable;
    }

    public bool parseHelper(int n, String[] lines, String regex, List<Message> messages,int[] errorLineNumber)
    {
        for (int i = 1; i <= n; i++)
        {
            String[] parts = lines[i].Split(lineSeparator, StringSplitOptions.None);
            if (parts.Length < 2)
            {
                errorLineNumber[1] = i;
                isParseable = false;
                break;
            }
            Message m = new Message();
            isParseable = parseProduction(parts[0], m);
            isParseable = isParseable && parseMessage(parts[1], m);

            if (parts.Length > 2)
            {
                isParseable = isParseable && parseDecision(parts[2], m);
            }

            if (true == isParseable)
                messages.Add(m);
            else
            {
                errorLineNumber[1] = i;
                isParseable = false;
                break;
            }
        }
        return isParseable;
    }
}