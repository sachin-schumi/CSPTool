using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class CSPGenerator
{
    public List<Message> messages = new List<Message>();

    public bool checkInput(String input)
    {
        InputParser parser = new InputParser();

        bool isParsed = parser.ParseFile(input, messages);

        if (false == isParsed)
        {
            // Get error
            Console.Error.WriteLine("");
            // exit
        }
        else
            generateGrammar(messages);
        return isParsed;
        //InputState inputState = parser.getInputState();
    }

    private String generateProcess(String s, String processName)
    {
        String[] parameters = Process.processMap[s];
        StringBuilder temp = new StringBuilder(processName + "(");
        for (int i = 0; i < parameters.Length; i++)
        {
            temp.Append(DataType.typeSet[parameters[i]]);
            if (i < parameters.Length - 1)
                temp.Append(",");
        }
        temp.Append(") = ");
        return temp.ToString();
    }


    private StringBuilder formMessage(Message m, StringBuilder output, 
        ref int msgVariable, StringBuilder variables)
    {
        StringBuilder temp = new StringBuilder("");
        if (m.Keys != null)
        {
            String key = m.Keys[0];
            variables.Append("var<" + Keys.encryption + "> msg" + msgVariable + ";\r\n");
            temp.Append("{msg" + msgVariable + "=new " + Keys.encryption + "(" + output + "," + key + ");}");
        }
        else
        {
            variables.Append("var msg" + msgVariable + ";\r\n");
            temp.Append("{msg" + msgVariable + " = "+ output +";}");
        }
        return temp;
    }


   

    private StringBuilder decryptMessage(Message m, StringBuilder output, 
        ref int genericVariable, StringBuilder variables,ref int pairVariable)
    {
        String key = m.Keys[1];
        if (m.MessageComponents.Count > 1)
        {
            genericVariable++;
            variables.Append("var<Pair> g" + genericVariable + ";\r\n");
            pairVariable = genericVariable;
        }
        else
        {
            genericVariable++;
            variables.Append("var g" + genericVariable + ";\r\n");
        }
        StringBuilder temp = new StringBuilder("{g" + genericVariable + "=new " + Keys.decryption + "(g" + (genericVariable - 1) + "," + key + ");} ->");
        return temp;
    }

    private StringBuilder formPair(StringBuilder existing, String newString)
    {
        StringBuilder output = new StringBuilder("new Pair(" + newString + "," + existing + ")");
        return output;
    }



    private StringBuilder removePair(List<String> messageComponents, String current
        , ref int genericVariable, StringBuilder variables, ref int i,ref int pairVariable)
    {
        StringBuilder output = new StringBuilder("{");
        for (; i < messageComponents.Count - 1; i++)
        {
            if (messageComponents[i] == current)
            {
                genericVariable++;
                variables.Append("var g" + genericVariable + ";\r\n");
                output.Append("g" + genericVariable + "=g" + (pairVariable) + ".getfirst();");
                break;
            }
            else
            {
                if (Keys.encryption.Length == 0)
                {
                    genericVariable++;
                    variables.Append("var<Pair> g" + genericVariable + ";\r\n");
                    output.Append("g" + genericVariable + "=g" + (genericVariable - 1) + ";");
                    pairVariable = genericVariable;
                    genericVariable++;
                    variables.Append("var g" + genericVariable + ";\r\n");
                }
                output.Append("g" + genericVariable + "=g" + pairVariable + ".getsecond();");
            }
        }
        output.Append("}");
        return output;
    }


    private void OutputChannel(Message m, Dictionary<String, StringBuilder> output,
        ref int genericVariable,ref int msgVariable, StringBuilder variables, HashSet<String> nextComponents
        , bool isDecrypted,ref int pairVariable)
    {
        String resp = m.Participators[1];
        List<String> messagecomponents = m.MessageComponents;
        StringBuilder temp = new StringBuilder("");

        if (isDecrypted == true)
        {
            temp = decryptMessage(m, temp, ref genericVariable, variables,ref pairVariable);
        }

        int k = 0;
        for (int j = 0; j < messagecomponents.Count; j++)
        {
            String mc = messagecomponents[j];
            String knownCheck = resp + " " + mc;
            if (nextComponents.Count > 0 && nextComponents.Contains(mc))
            {
                if (!(Process.aliases.ContainsKey(knownCheck)))
                {
                    if (messagecomponents.Count > 1)
                    {
                        temp.Append(removePair(messagecomponents, mc, ref genericVariable, variables,ref k,ref pairVariable));
                    }
                    String newVar = "g" + genericVariable;
                    Process.aliases[resp + " " + mc] = newVar;
                    genericVariable++;
                }
            }
        }
        if(temp.Length > 0)
            output[resp].Append(temp + "->");
    }

    private void InputChannel(Message m, Dictionary<String, StringBuilder> output,
        ref int genericVariable,ref int msgVariable, StringBuilder variables)
    {
        String ini = m.Participators[0];
        List<String> messagecomponents = m.MessageComponents;
        StringBuilder temp = new StringBuilder("");
        for (int j = messagecomponents.Count - 1; j >= 0; j--)
        {
            String mc = messagecomponents[j];
            String knownCheck = ini + " " + mc;
            if (Process.aliases.ContainsKey(knownCheck))
            {
                if (temp.Length > 0)
                    temp = formPair(temp, Process.aliases[knownCheck]);
                else
                    temp.Append(Process.aliases[knownCheck]);
            }
            else
            {
                String newVar = "g" + genericVariable;
                genericVariable++;
                Process.aliases[ini + " " + mc] = newVar;
                if (!(temp.Equals("")))
                    temp = formPair(temp, newVar);
                else
                    temp.Append(newVar);
            }
        }
        
        temp = formMessage(m, temp, ref msgVariable, variables);
        
        output[ini].Append(temp + "->");
    }

    public void generateGrammar(List<Message> messages)
    {
        StringBuilder variables = new StringBuilder("");
        Dictionary<String, StringBuilder> output = new Dictionary<String, StringBuilder>();

        StringBuilder enumVariables = new StringBuilder("");

        variables.Append("#import \"Lib_v0+equal+know\";\r\n\r\n");

        foreach (KeyValuePair<String, String> entry in DataType.typeSet)
        {
            if (entry.Value == "Nonce")
            {
                variables.Append("var " + entry.Key + " = new Nonce();\r\n");
            }
            else
            {
                enumVariables.Append(entry.Key);
                enumVariables.Append(",");
            }
        }

        if (Keys.encryption == "AEnc")
        {
            foreach (KeyValuePair<String, String[]> entry in Keys.keyMap)
            {
                String[] k = entry.Value;
                variables.Append("var<SKey> " + k[1] + "= new SKey();");
                variables.Append("var<PKey> " + k[0] + "= new PKey(" + k[1] + ");");
            }
        }
        else if (Keys.encryption == "SEnc")
        {
            foreach (KeyValuePair<String, String[]> entry in Keys.keyMap)
            {
                String[] k = entry.Value;
                variables.Append("var<SKey> " + k[1] + "= new SKey();");
            }
        }


        if (!(enumVariables.Equals("")))
        {
            enumVariables = new StringBuilder("enum{" + enumVariables);
            enumVariables.Remove(enumVariables.Length - 1, 1);
            enumVariables.Append("};");
            variables.Append(enumVariables);
        }
        variables.Append("\r\n\r\n");
        variables.Append("channel ca 0;");
        variables.Append("\r\n\r\n");

        String ini = messages[0].Participators[0];
        output[ini] = new StringBuilder("");
        String process = generateProcess(ini, Process.processName[ini]);
        output[ini].Append(process);
        String resp = messages[0].Participators[1];
        output[resp] = new StringBuilder("");
        process = generateProcess(resp, Process.processName[resp]);
        output[resp].Append(process);

        int genericVariable = 1;
        int msgVariable = 1;
        int pairVariable = 1;
        for (int i = 0; i < messages.Count; i++)
        {
            Message m = messages[i];
            ini = m.Participators[0];
            resp = m.Participators[1];
            InputChannel(m, output, ref genericVariable, ref msgVariable, variables);
            output[ini].Append(m.channel + m.InputChannel + ".msg" + msgVariable + "->");
            msgVariable++;
            output[resp].Append(m.channel + m.OutputChannel + ".g" + genericVariable + "->");
            HashSet<String> components = new HashSet<String>();
            if (i + 1 < messages.Count)
                components = new HashSet<String>(messages[i + 1].MessageComponents);
            bool isDecrypted = false;
            if (m.Keys != null)
                isDecrypted = true;
            OutputChannel(m, output,ref genericVariable,ref msgVariable, variables, components, isDecrypted, ref pairVariable);
        }
        File.AppendAllText(@"C:\FSDT\file.csp", variables + Environment.NewLine);
        List<StringBuilder> outputValues = new List<StringBuilder>(output.Values);
        foreach (StringBuilder s in outputValues)
        {
            String s1 = s + "Skip;";
            File.AppendAllText(@"C:\FSDT\file.csp", s1 + "\r\n\r\n");
        }
    }
}
