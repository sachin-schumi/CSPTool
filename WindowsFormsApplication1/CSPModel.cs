﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PAT.Common;
using OutOfMemoryException = PAT.Common.Classes.Expressions.ExpressionClass.OutOfMemoryException;
using PAT.Common.Classes.Expressions.ExpressionClass;
using PAT.Common.Classes.ModuleInterface;
using System.Text.RegularExpressions;
using System.Reflection;

namespace WindowsFormsApplication1
{
    class CSPModel
    {
        List<string> channels;
        HashSet<string> allMessages;
        List<string> constantMessages;
        string[] simpleModelLines;
        string fullModelPath;

        const string LIB_NAME = "Lib_v0+equal+know.dll";
        const string CHANNEL = "channel";
        const string ATTACKER_PROCESS = "AutoGeneratedAttacker()";
        const string ATTACKER_KNOWLEDGE_VARIABLE_NAME = "AutoGeneratedAttackerKnowledge";

        public CSPModel(string inputFile, string outputFile)
        {
            channels = new List<string>();
            //messages = new Dictionary<string, List<string>>();
            allMessages = new HashSet<string>();
            constantMessages = new List<string>();
            simpleModelLines = File.ReadAllLines(inputFile);
            fullModelPath = outputFile;
        }

        private Error AnalyzeModel(string mainProcessName)
        {
            Error e = new Error(0,false);
            List<string> simpleModelList = new List<string>();
            int attackerAddedCounter = 0;
            for (int i = 0; i < simpleModelLines.Length; i++)
            {
                // remove comments and leading and trailing white-space
                string line = simpleModelLines[i];
                int commentIndex = line.IndexOf("//");
                if (commentIndex >= 0)
                {
                    line = line.Substring(0, commentIndex);
                }
                line = line.Trim();
                // handle mulptile statement in a line sperated by ';'
                string[] multiStatements = line.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string stat in multiStatements)
                {
                    string statement = stat.Trim();
                    simpleModelList.Add(statement);

                    if (statement.StartsWith(mainProcessName))
                    {
                        int equalIndex = statement.IndexOf("=");
                        if (equalIndex > 0)
                        {
                            //check if this statement defines the main process
                            if (statement.Substring(0, equalIndex).Split(new string[] { " ", mainProcessName }, StringSplitOptions.RemoveEmptyEntries).Length == 0)
                            {
                                string searchString = statement.Substring(0, equalIndex + 1); // string from mainProcessName to equal sign
                                string fullLine = simpleModelLines[i];
                                int searchIndex = fullLine.IndexOf(searchString);

                                if (searchIndex >= 0 && (searchIndex < commentIndex || commentIndex < 0))
                                {
                                    int breakingIndex = searchIndex + searchString.Length; // break line at index of equal sign + 1
                                    simpleModelLines[i] = fullLine.Substring(0, breakingIndex) + ATTACKER_PROCESS + " ||" + fullLine.Substring(breakingIndex);
                                    attackerAddedCounter++;
                                }
                            }
                        }
                    }
                }
            }

            if (attackerAddedCounter != 1)
            {
                string errMsg = ATTACKER_PROCESS + " is appended as a parallel process " + attackerAddedCounter + " time(s)! Expected 1 time exactly!\nYou may provided a wrong main process name! Stop Processing!";
                //Debug.Assert(false, errMsg);
                e.Message = errMsg;
                e.IsError = true;
                return e;
            }

            // load the cryptographic primitive library to get the unittype type list
            // load it in a separate domain, so that later pat module can still load it to verify csp model
            AppDomain getUnitypeTypeListDomain = AppDomain.CreateDomain("GetUnitypeTypeListDomain");
            Type type = typeof(GetTypeListProxy);
            GetTypeListProxy getTypeListProxy = (GetTypeListProxy)getUnitypeTypeListDomain.CreateInstanceAndUnwrap(
                type.Assembly.FullName,
                type.FullName);

            HashSet<string> typeList = getTypeListProxy.GetUnitypeTypeList(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib", LIB_NAME));
            AppDomain.Unload(getUnitypeTypeListDomain);

            foreach (string line in simpleModelList)
            {
                if (line.StartsWith(CHANNEL))
                {
                    string[] tokens = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens[0].Equals(CHANNEL) && tokens.Length > 1)
                    {
                        channels.Add(tokens[1]);
                    }
                }
                else if (line.StartsWith("var<")) // global variable
                {
                    string[] tokens = line.Split(new string[] { " ", ";", "=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length > 1)
                    {
                        // the first element is the type declaration
                        // the second element is the variable name
                        string typeName = Regex.Match(tokens[0], @"<([^>]*)>").Groups[1].Value;
                        if (typeName.Equals("Constant"))
                            constantMessages.Add(tokens[1]);
                        else if (typeList.Contains(typeName))
                            allMessages.Add(tokens[1]);
                    }
                }
            }
            return e;
        }

        public Error ToFullModel(string mainProcessName)
        {
            Error e = new Error(0,false);
            e = AnalyzeModel(mainProcessName);
            if (AnalyzeModel(mainProcessName).IsError)
                return e;

            List<string> fullModelLines = new List<string>();
            //fullModelLines.Add("#import \"test\";");
            fullModelLines.AddRange(simpleModelLines);
            fullModelLines.Add("var<Knowledge> " + ATTACKER_KNOWLEDGE_VARIABLE_NAME + ";");

            List<string> channelAttTexts = new List<string>();
            List<string> resendAttTexts = new List<string>();

            foreach (var channel in channels)
            {
                channelAttTexts.Add(channel + "?MSGZ{" + ATTACKER_KNOWLEDGE_VARIABLE_NAME + ".addKnowledge(MSGZ)}->" + ATTACKER_PROCESS);

                foreach (var constant in constantMessages)
                    resendAttTexts.Add("           []" + channel + "!" + constant + "->" + ATTACKER_PROCESS);

                foreach (var msg in allMessages)
                    resendAttTexts.Add("           []if (" + ATTACKER_KNOWLEDGE_VARIABLE_NAME + ".knows(" + msg + ")==true){" + channel + "!" + msg + "->" + ATTACKER_PROCESS + "}else{" + ATTACKER_PROCESS + "}");
            }

            if (channelAttTexts.Count > 0)
            {
                fullModelLines.Add(ATTACKER_PROCESS + "= " + channelAttTexts[0]);

                for (int i = 1; i < channelAttTexts.Count; i++)
                    fullModelLines.Add("           []" + channelAttTexts[i]);

                fullModelLines.AddRange(resendAttTexts);

                string last = fullModelLines.Last();
                if (!last.EndsWith(";"))
                {
                    last += ";";
                    fullModelLines.RemoveAt(fullModelLines.Count - 1);
                    fullModelLines.Add(last);
                }
            }

            File.WriteAllLines(fullModelPath, fullModelLines);
            return e;
        }

        public Error Verify()
        {
            Error e = new Error(0, false);
            try
            {
                ModuleFacadeBase modulebase = PAT.Common.Ultility.Ultility.LoadModule("CSP");
                SpecificationBase Spec = modulebase.ParseSpecification(File.ReadAllText(fullModelPath), "", fullModelPath);
                string resultMsg = "";
                foreach (var assertion in Spec.AssertionDatabase.Values)
                {
                    Console.WriteLine("Verifying the assertion: " + assertion.ToString());
                    // Apply verification settings
                    assertion.UIInitialize(null, 0, 0);
                    //Start the verification
                    assertion.InternalStart();
                    if (assertion.VerificationOutput.VerificationResult.Equals(VerificationResultType.INVALID))
                    {
                        resultMsg += "The assertion is invalid: " + assertion.ToString() + "\n";

                        if (assertion.VerificationOutput.CounterExampleTrace != null)
                        {
                            if (assertion.VerificationOutput.CounterExampleTrace.Count > 0)
                            {
                                //Get the counterexample trace
                                resultMsg += "Counter Example Trace: ";
                                foreach (ConfigurationBase step in assertion.VerificationOutput.CounterExampleTrace)
                                {
                                    resultMsg += "->" + step.GetDisplayEvent();
                                }
                                resultMsg += "\n";
                            }
                        }
                    }
                    else if (assertion.VerificationOutput.VerificationResult.Equals(VerificationResultType.VALID))
                        resultMsg += "The assertion is valid: " + assertion.ToString() + "\n";
                    else
                        resultMsg += "The assertion could not be verified: " + assertion.ToString() + "\n";
                    resultMsg += "\n";
                }
                //System.Windows.MessageBox.Show(resultMsg, "Refinement and Verification Completed!");
            }
            catch (RuntimeException ex)
            {
                string runtimeErrMsg = "Runtime exception occurred: " + ex.Message + "\n";
                //Out of memory Exception
                if (ex is OutOfMemoryException)
                {
                    runtimeErrMsg += "Model is too big, out of memory.";
                }
                else
                {
                    runtimeErrMsg += "Check your input model for the possiblity of errors.";
                }
                e.Message = runtimeErrMsg;
                e.IsError = true;
            }
            //General Exceptions
            catch (Exception ex)
            {
                e.Message = "Error occurred: " + ex.Message;
                e.IsError = true;
            }
            return e;
        }
    }

    // Proxy class to get unitype typelist
    public class GetTypeListProxy : MarshalByRefObject
    {
        public HashSet<string> GetUnitypeTypeList(string assemblyPath)
        {
            HashSet<string> typeList = new HashSet<string>(new string[] { "Unitype", "Key", "Bitstring" });
            try
            {
                Assembly myAssembly = Assembly.LoadFile(assemblyPath);
                foreach (Type t in myAssembly.GetTypes())
                {
                    if (typeList.Contains(t.BaseType.Name.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last()))
                        typeList.Add(t.Name.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last());
                }
                return typeList;
            }
            catch (Exception)
            {
                return typeList;
            }
        }
    }
}
