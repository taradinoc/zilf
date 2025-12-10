using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Zifmia.FyreVM.Service;

namespace FyreVMDemo.Game
{
    public class GlulxStateService
    {
        #region Singleton Pattern

        protected static GlulxStateService _instance;

        public static GlulxStateService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GlulxStateService();
                }

                return _instance;
            }
        }
        #endregion

        private Dictionary<eGlulxCommands, string> _dGlulxCommands = new Dictionary<eGlulxCommands, string>();
        private EngineWrapper _fyreVmWrapper;

        private bool _bShutdown = false;

        public GlulxStateService()
        {
            _dGlulxCommands.Add(eGlulxCommands.Look, "look");
            _dGlulxCommands.Add(eGlulxCommands.Wait, "wait");
        }

        public void Initialize(object data)
        {
            if (data == null)
            {
                return;
            }

            var ulxFile = data as TextAsset;
            if(ulxFile == null)
            {
                return;
            }

            var buffer = ulxFile.bytes;

            _fyreVmWrapper = new EngineWrapper(buffer);

            _bShutdown = false;
        }

        public void Shutdown()
        {
            _bShutdown = true;
        }

        public void InitialScene()
        {
            _fyreVmWrapper.SendCommand(_dGlulxCommands[eGlulxCommands.Look]);

            ProcessLocationGlulxOutput(_fyreVmWrapper.FromHash());
        }

        public void Look()
        {
            if (_bShutdown == true)
            {
                return;
            }

            _fyreVmWrapper.SendCommand(_dGlulxCommands[eGlulxCommands.Look]);

            this.ProcessGlulxOutput(_fyreVmWrapper.FromHash());
        }

        public void GoToDirection(string sMessage)
        {
            _fyreVmWrapper.SendCommand(sMessage);

            this.ProcessLocationGlulxOutput(_fyreVmWrapper.FromHash());
        }

        public void Wait()
        {
            _fyreVmWrapper.SendCommand(_dGlulxCommands[eGlulxCommands.Wait]);

            this.ProcessGlulxOutput(_fyreVmWrapper.FromHash());
        }

        #region ParsingSpecificGlulxChannels

        private void ParseLocation(string sValue)
        {
            try
            {
                var sParsed = sValue.Split(':');

                if (sParsed.Length != 1) return;

                var sLevelID = sParsed[0].ToUpperInvariant();

                TransitionController.Instance.Transition(sLevelID);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        #endregion

        private string GetOutputChannelString(Dictionary<string, string> outputChannels, eStateOutputChannels eChannel)
        {
            var sChannel = GameConstants.OutputChannelToString(eChannel);

            if (outputChannels.ContainsKey(sChannel) == false)
            {
                return String.Empty;
            }

            return outputChannels[sChannel];
        }

        private void ProcessGlulxOutput(Dictionary<string, string> outputChannels)
        {
            if (_bShutdown == true)
            {
                return;
            }

            var sChannelOutput = "";

            sChannelOutput = this.GetOutputChannelString(outputChannels, eStateOutputChannels.EndGame);

            this.ParseEndEpisode(sChannelOutput);

            sChannelOutput = this.GetOutputChannelString(outputChannels, eStateOutputChannels.Location);

            if (string.IsNullOrEmpty(sChannelOutput) == false)
            {
                this.ParseLocation(sChannelOutput);
            }
        }

        private void ParseEndEpisode(string sChannelOutput)
        {
            if (string.IsNullOrEmpty(sChannelOutput) == false)
            {
                //stateEndEpisodeSignal.Dispatch();
            }
        }

        private void ProcessLocationGlulxOutput(Dictionary<string, string> outputChannels)
        {
            if (_bShutdown == true)
            {
                return;
            }

            var sChannelOutput = this.GetOutputChannelString(outputChannels, eStateOutputChannels.Location);

            if (string.IsNullOrEmpty(sChannelOutput) == false)
            {
                this.ParseLocation(sChannelOutput);
            }
        }
    }
}