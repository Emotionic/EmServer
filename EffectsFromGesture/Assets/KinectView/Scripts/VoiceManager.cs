using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using Windows.Kinect;


public class VoiceManager : MonoBehaviour
{
    private string[] m_Keywords = { "たまや" ,"焼肉定食", "ラーメン", "仙台高専", "プロコン", "山口", "与謝野晶子", "DDR", "サウンドボルテックス","野獣先輩", "810", "1 1 4 5 1 4"};
    private KeywordRecognizer m_Recognizer;

    // Use this for initialization
    void Start()
    {
        m_Recognizer = new KeywordRecognizer(m_Keywords);
        m_Recognizer.OnPhraseRecognized += OnPhraseRecognized;
        m_Recognizer.Start();
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
        builder.AppendFormat("\tTimestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
        builder.AppendFormat("\tDuration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
        Debug.Log(builder.ToString());

        switch(args.text)
        {
            case "たまや":
                Debug.Log("やったぜ");
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

}