using System.Collections.Generic;
using System.Threading;
using System.IO;
using System;
using TrueGearSDK;
using Newtonsoft.Json;
using System.Linq;
using ArizonaSunshine2_TrueGear;


namespace MyTrueGear
{
    public class TrueGearMod
    {
        private static TrueGearPlayer _player = null;

        private static ManualResetEvent heartbeatMRE = new ManualResetEvent(false);
        private static ManualResetEvent lefthandstrokebuddyMRE = new ManualResetEvent(false);
        private static ManualResetEvent righthandstrokebuddyMRE = new ManualResetEvent(false);


        public void HeartBeat()
        {
            while(true)
            {
                heartbeatMRE.WaitOne();
                _player.SendPlay("HeartBeat");
                Thread.Sleep(600);
            }            
        }
        public void LeftHandStrokeBuddy()
        {
            while (true)
            {
                lefthandstrokebuddyMRE.WaitOne();
                _player.SendPlay("LeftHandStrokeBuddy");
                Thread.Sleep(200);
            }
        }
        public void RightHandStrokeBuddy()
        {
            while (true)
            {
                righthandstrokebuddyMRE.WaitOne();
                _player.SendPlay("RightHandStrokeBuddy");
                Thread.Sleep(200);
            }
        }

        public TrueGearMod() 
        {
            _player = new TrueGearPlayer("1540210","ArizonaSunShine2");
            _player.PreSeekEffect("DefaultDamage");
            _player.Start();
            new Thread(new ThreadStart(this.HeartBeat)).Start();
            new Thread(new ThreadStart(this.LeftHandStrokeBuddy)).Start();
            new Thread(new ThreadStart(this.RightHandStrokeBuddy)).Start();
        }    


        public void Play(string Event)
        { 
            _player.SendPlay(Event);
        }

        public void PlayAngle(string tmpEvent, float tmpAngle, float tmpVertical)
        {
            try
            {
                float angle = (tmpAngle - 22.5f) > 0f ? tmpAngle - 22.5f : 360f - tmpAngle;
                int horCount = (int)(angle / 45) + 1;

                int verCount = tmpVertical > 0.1f ? -4 : tmpVertical < -0.5f ? 8 : 0;


                EffectObject oriObject = _player.FindEffectByUuid(tmpEvent);

                EffectObject rootObject = EffectObject.Copy(oriObject);


                foreach (TrackObject track in rootObject.trackList)
                {
                    if (track.action_type == ActionType.Shake)
                    {
                        for (int i = 0; i < track.index.Length; i++)
                        {
                            if (verCount != 0)
                            {
                                track.index[i] += verCount;
                            }
                            if (horCount < 8)
                            {
                                if (track.index[i] < 50)
                                {
                                    int remainder = track.index[i] % 4;
                                    if (horCount <= remainder)
                                    {
                                        track.index[i] = track.index[i] - horCount;
                                    }
                                    else if (horCount <= (remainder + 4))
                                    {
                                        var num1 = horCount - remainder;
                                        track.index[i] = track.index[i] - remainder + 99 + num1;
                                    }
                                    else
                                    {
                                        track.index[i] = track.index[i] + 2;
                                    }
                                }
                                else
                                {
                                    int remainder = 3 - (track.index[i] % 4);
                                    if (horCount <= remainder)
                                    {
                                        track.index[i] = track.index[i] + horCount;
                                    }
                                    else if (horCount <= (remainder + 4))
                                    {
                                        var num1 = horCount - remainder;
                                        track.index[i] = track.index[i] + remainder - 99 - num1;
                                    }
                                    else
                                    {
                                        track.index[i] = track.index[i] - 2;
                                    }
                                }
                            }
                        }
                        if (track.index != null)
                        {
                            track.index = track.index.Where(i => !(i < 0 || (i > 19 && i < 100) || i > 119)).ToArray();
                        }
                    }
                    else if (track.action_type == ActionType.Electrical)
                    {
                        for (int i = 0; i < track.index.Length; i++)
                        {
                            if (horCount <= 4)
                            {
                                track.index[i] = 0;
                            }
                            else
                            {
                                track.index[i] = 100;
                            }
                            if (horCount == 1 || horCount == 8 || horCount == 4 || horCount == 5)
                            {
                                track.index = new int[2] { 0, 100 };
                            }

                        }
                    }
                }
                _player.SendPlayEffectByContent(rootObject);

            }
            catch (System.Exception ex)
            {
                _player.SendPlay(tmpEvent);
                Plugin.Log.LogError(ex);
            }
        }



        public void StartHeartBeat()
        {
            heartbeatMRE.Set();
        }

        public void StopHeartBeat()
        {
            heartbeatMRE.Reset();
        }

        public void StartLeftHandStrokeBuddy()
        {
            lefthandstrokebuddyMRE.Set();
        }

        public void StopLeftHandStrokeBuddy()
        {
            lefthandstrokebuddyMRE.Reset();
        }

        public void StartRightHandStrokeBuddy()
        {
            righthandstrokebuddyMRE.Set();
        }

        public void StopRightHandStrokeBuddy()
        {
            righthandstrokebuddyMRE.Reset();
        }

    }
}
