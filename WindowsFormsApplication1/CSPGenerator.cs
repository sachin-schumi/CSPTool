using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Text;

class Error
{
    private int lineNumber;

    public int LineNumber
    {
        get { return lineNumber; }
        set { lineNumber = value; }
    }
    bool isError;

    private String message;

    public String Message
    {
        get { return message; }
        set { message = value; }
    }

    public bool IsError
    {
        get { return isError; }
        set { isError = value; }
    }

    public Error(int lineNumber,bool isError)
    {
        this.lineNumber = lineNumber;
        this.isError = isError;
    }
}

class CSPGenerator
{
    public List<Message> messages = new List<Message>();

    private String format(String input)
    {
        char tab = '\u0009';
        input = input.Replace(tab.ToString(), " ");

        String[] patterns = {"[ ]{2,}",@"[\s]*(:)[\s]*",@"[\s]*(->)[\s]*",@"[\s]*(\|)[\s]*",@"[\s]*(,)[\s]*"
                            ,@"[\s]*({)[\s]*",@"[\s]*(})",@"[\s]*(==)[\s]*",@"[\s]*(&&)[\s]*",@"[\s]*(!=)[\s]*"};
        String[] replacements = {" ",":","->","|",",","{","}","==","&&","!="};

        for(int i = 0; i < patterns.Length; i++)
        {
            Regex regex = new Regex(patterns[i], RegexOptions.None);
            input = regex.Replace(input, replacements[i]);
        }
        return input;
    }

    public Error checkInput(String input)
    {

        input = format(input);
        InputParser parser = new InputParser();

        bool isParsed = parser.ParseFile(input, messages);

        Error ex = new Error(0,false);

        if (false == isParsed)
        {
            ex.LineNumber = parser.errorlineNumber;
            ex.IsError = true;
            return ex;
        }
        else
            generateGrammar(messages);
        ex.IsError = false;
        return ex;
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
            variables.Append("var<Unitype> msg" + msgVariable + ";\r\n");
            temp.Append("{msg" + msgVariable + " = "+ output +";}");
        }
        return temp;
    }


   

    private StringBuilder decryptMessage(Message m, StringBuilder output, 
        ref int genericVariable, StringBuilder variables,ref int pairVariable)
    {
        String key = m.Keys[1];
        genericVariable++;
        variables.Append("var<" + Keys.encdec[Keys.encryption] + "> g" + genericVariable + ";\r\n");       
        StringBuilder temp = new StringBuilder("{g" + genericVariable + "=new " + Keys.decryption + "(g" + (genericVariable - 1) + "," + key + ");} ->");
        return temp;
    }

    private StringBuilder formPair(StringBuilder existing, String newString)
    {
        StringBuilder output = new StringBuilder("new Pair(" + newString + "," + existing + ")");
        return output;
    }


    private void createPairvar(ref int genericVariable, ref int pairVariable,StringBuilder variables)
    {
        genericVariable++;
        variables.Append("var<Pair> g" + genericVariable + ";\r\n");        
        pairVariable = genericVariable;
    }

    private void getSingleFromPair(ref int genericVariable, ref int pairVariable, StringBuilder output,String flag
        ,StringBuilder variables,ref int i)
    {
        genericVariable++;
        variables.Append("var<Unitype> g" + genericVariable + ";\r\n");
        if (flag == "second")
        {
            output.Append("g" + genericVariable + "=g" + (pairVariable) + ".getsecond();");
        }
        else
        {
            output.Append("g" + genericVariable + "=g" + (pairVariable) + ".getfirst();");
        }
    }

    private StringBuilder removePair(List<String> messageComponents, String current
        , ref int genericVariable, StringBuilder variables, ref int i,ref int pairVariable,
        ref bool isChannelData,bool isDecrypted)
    {
        StringBuilder output = new StringBuilder("");
        if(i == 0)
            createPairvar(ref genericVariable,ref pairVariable,variables);
        if (isChannelData == true)
        {
            if(isDecrypted == true)
                output.Append("g" + genericVariable + "=g" + (genericVariable - 1) + ".result();");
            else
                output.Append("g" + genericVariable + "=g" + (genericVariable - 1) + ";");
        }
        bool isFound = false;
        for (; i < messageComponents.Count - 2; i++)
        {
            if (messageComponents[i] == current)
            {
                isFound = true;
                getSingleFromPair(ref genericVariable,ref pairVariable,output,"first",variables,ref i);
                output.Append("g" + pairVariable + "=g" + pairVariable + ".getsecond();");
                break;
            }
            else
            { 
                output.Append("g" + pairVariable + "=g" + pairVariable + ".getsecond();");
            }
        }
        if (isFound == false)
        { 
            if(messageComponents[i] == current)
                getSingleFromPair(ref genericVariable, ref pairVariable, output, "first", variables, ref i); 
            else
                getSingleFromPair(ref genericVariable, ref pairVariable, output, "second", variables, ref i); 
        }
        return output;
    }


    private void OutputChannel(Message m, Dictionary<String, StringBuilder> output,
        ref int genericVariable,ref int msgVariable, StringBuilder variables, HashSet<String> nextComponents
        , bool isDecrypted,ref int pairVariable)
    {
        String resp = m.Participators[1];
        List<String> messagecomponents = m.MessageComponents;
        StringBuilder temp = new StringBuilder("");
        StringBuilder decryptedMessage = new StringBuilder("");
        if (isDecrypted == true)
        {
            decryptedMessage = decryptMessage(m, temp, ref genericVariable, variables, ref pairVariable);
        }

        int k = 0;
        bool isChannelData = true;
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
                        temp.Append(removePair(messagecomponents, mc, ref genericVariable, variables
                            , ref k, ref pairVariable, ref isChannelData,isDecrypted));
                        if (isChannelData == true)
                            isChannelData = false;
                    }
                    else
                    {
                        if (isChannelData == true)
                        {
                            genericVariable++;
                            variables.Append("var<Unitype> g" + genericVariable + ";\r\n");
                            if(isDecrypted == true)
                            {
                                temp.Append("g" + genericVariable + "=g" + (genericVariable - 1) + ".result();");
                            }
                            else
                            {
                                temp.Append("g" + genericVariable + "=g" + (genericVariable - 1) + ";");
                            }
                            isChannelData = false;
                        }
                        else
                        {
                            genericVariable++;
                        }
                    }
                    String newVar = "g" + genericVariable;
                    Process.aliases[resp + " " + mc] = newVar;
                }
            }
        }
        if (temp.Length > 0)
        {
            if (isDecrypted == true)
            {
                output[resp].Append(decryptedMessage);
            }
            output[resp].Append("{" + temp + "}" + " -> ");
        }
    }

    private void InputChannel(Message m, Dictionary<String, StringBuilder> output,
        ref int genericVariable,ref int msgVariable, StringBuilder variables)
    {
        int tempV = genericVariable;
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
        if (tempV == genericVariable)
            genericVariable++;
        output[ini].Append(temp + " -> ");
    }

    private void print(StringBuilder variables, Dictionary<String, StringBuilder>  output)
     {
         System.IO.File.WriteAllText(@"C:\FSDT\test.csp", string.Empty);
         File.AppendAllText(@"C:\FSDT\test.csp", variables + Environment.NewLine);
         List<StringBuilder> outputValues = new List<StringBuilder>(output.Values);
         foreach (StringBuilder s in outputValues)
         {
             File.AppendAllText(@"C:\FSDT\test.csp", s + "\r\n\r\n");
         }
     }


    private String getRHSvariable(String rhs)
    {
        String[] parts = rhs.Split('.');
        String output = "";
        if (parts[0] == "Process")
            output = parts[1];
        else
        {
            output = Process.aliases[parts[0].Trim() + " " + parts[1].Trim()];
        }
        return output;
    }

    private StringBuilder generateDecisions(Message m, StringBuilder variables, ref int decisionVariable)
    {
        StringBuilder output = new StringBuilder("");
        output.Append("{if(");
        List<String[]> decisions = m.Decisions;
        if(m.Comparison == "==")
            variables.Append("var D" + decisionVariable + " = false;\r\n");
        else
            variables.Append("var D" + decisionVariable + " = true;\r\n");
        for (int i = 0; i < decisions.Count; i++)
        {
            String datatype = DataType.typeSet[decisions[i][0]];
            String lhs = decisions[i][0];
            String rhs = decisions[i][1];
            output.Append(lhs+".");
            if(datatype.IndexOf("Nonce") >= 0)
            {
                output.Append("Nonceequal");
            }
            else
            {
                output.Append("Constantequal");
            }
            String rhsVariable = getRHSvariable(rhs);
            output.Append("(" + rhsVariable + ")");
            if (i < decisions.Count - 1)
                output.Append(" && ");
        }
        if (m.Comparison == "==")
            output.Append("){ D"+decisionVariable + " = true;}} -> ");
        else
            output.Append("){ D" + decisionVariable + " = false;}} -> ");
        return output;
    }


    public void generateGrammar(List<Message> messages)
    {
        StringBuilder variables = new StringBuilder("");
        Dictionary<String, StringBuilder> output = new Dictionary<String, StringBuilder>();

        variables.Append("#import \"Lib_v0+equal+know\";\r\n\r\n");

        try
        {

            foreach (KeyValuePair<String, String> entry in DataType.typeSet)
            {
                if (entry.Value.IndexOf("Nonce") >= 0)
                {
                    variables.Append("var " + entry.Key + " = new Nonce();\r\n");
                }
                else
                {
                    variables.Append("var " + entry.Key + " = new Constant();\r\n");
                }
            }

            if (Keys.encryption == "AEnc")
            {
                foreach (KeyValuePair<String, String[]> entry in Keys.keyMap)
                {
                    String[] k = entry.Value;
                    variables.Append("var<SKey> " + k[1] + "= new SKey();\r\n");
                    variables.Append("var<PKey> " + k[0] + "= new PKey(" + k[1] + ");\r\n");
                }
            }
            else if (Keys.encryption == "SEnc")
            {
                foreach (KeyValuePair<String, String[]> entry in Keys.keyMap)
                {
                    String[] k = entry.Value;
                    variables.Append("var<SKey> " + k[1] + "= new SKey();\r\n");
                    break;
                }
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
            int decisionVariable = 1;
            print(variables, output);
            for (int i = 0; i < messages.Count; i++)
            {
                Message m = messages[i];
                ini = m.Participators[0];
                resp = m.Participators[1];
                InputChannel(m, output, ref genericVariable, ref msgVariable, variables);
                output[ini].Append(m.channel + m.InputChannel + ".msg" + msgVariable + " -> ");
                if (m.DecisionOrigin == ini)
                {
                    output[ini].Append(generateDecisions(m, variables, ref decisionVariable));
                    decisionVariable++;
                }
                print(variables, output);
                msgVariable++;
                output[resp].Append(m.channel + m.OutputChannel + ".g" + genericVariable + " -> ");
                HashSet<String> components = new HashSet<String>();
                if (i + 1 < messages.Count)
                    components = new HashSet<String>(messages[i + 1].MessageComponents);
                bool isDecrypted = false;
                if (m.Keys != null)
                    isDecrypted = true;
                OutputChannel(m, output, ref genericVariable, ref msgVariable, variables, components, isDecrypted, ref pairVariable);
                if (m.DecisionOrigin == resp)
                {
                    output[resp].Append(generateDecisions(m, variables, ref decisionVariable));
                    decisionVariable++;
                }
                print(variables, output);

            }
            StringBuilder success = new StringBuilder("");
            if (decisionVariable > 1)
            {
                success = new StringBuilder("#define success {");

                for (int i = 1; i <= decisionVariable - 1; i++)
                {
                    success.Append("D" + i + " == true");
                    if (i <= decisionVariable - 2)
                        success.Append(" && ");
                }
                success.Append("};\r\n#assert Protocol reaches success;\r\n");
            }
            File.AppendAllText(@"C:\FSDT\file.csp", variables + Environment.NewLine);
            StringBuilder protocol = new StringBuilder("Protocol = ");
            int count = 0;
            foreach (KeyValuePair<String, String[]> entry in Process.protocol)
            {
                String[] data = entry.Value;
                protocol.Append(entry.Key + "(" + String.Join(",", data) + ")");
                if (count < Process.protocol.Count - 1)
                    protocol.Append(" ||| ");
                count++;
            }
            protocol.Append(";\r\n\r\n #assert Protocol deadlockfree;\r\n");

            List<StringBuilder> outputValues = new List<StringBuilder>(output.Values);
            foreach (StringBuilder s in outputValues)
            {
                String s1 = s + "Skip;";
                File.AppendAllText(@"C:\FSDT\file.csp", s1 + "\r\n\r\n");
            }
            File.AppendAllText(@"C:\FSDT\file.csp", protocol + "\r\n");
            File.AppendAllText(@"C:\FSDT\file.csp", success + "\r\n");
        }
        catch (Exception e)
        { 
        }
    }
}
