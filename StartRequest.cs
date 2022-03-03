using System.Collections.Generic;

namespace Symbl.Realtime.Audio
{
    public class SpeechRecognition
    {
        public string encoding { get; set; }
        public int sampleRateHertz { get; set; }
    }

    public class Config
    {
        public double confidenceThreshold { get; set; }
        public string languageCode { get; set; }
        public SpeechRecognition speechRecognition { get; set; }
    }

    public class Speaker
    {
        public string userId { get; set; }
        public string name { get; set; }
    }

    public class StartRequest
    {
        public string type { get; set; }
        public string meetingTitle { get; set; }
        public List<string> insightTypes { get; set; }
        public Config config { get; set; }
        public Speaker speaker { get; set; }
    }
}
