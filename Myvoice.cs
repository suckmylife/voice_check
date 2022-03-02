using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class Myvoice : MonoBehaviour
{
    public float sensitivity = 100;
    public float loudness = 0;
    public float pitch = 0;

    AudioSource _audio;

    public float RmsValue;
    public float DbValue;
    public float PitchValue;

    private const int QSamples = 1024;
    private const float RefValue = 0.1f;
    private const float Threshold = 0.02f;

    float[] _samples;
    private float[] _spectrum;
    private float _fSample;

    public bool startMicOnStartup = true;

    public bool stopMicrophoneListener = false; 
    public bool startMicrophoneListener = false;
    private bool microphoneListenerOn = false;
    public bool disableOutputSound = false;

    AudioSource src;
    public AudioMixer masterMixer;

    float timesinceRestart = 0;

    Renderer eyecolor;
    public GameObject game;

    private void Start()
    {
        eyecolor = game.GetComponent<Renderer>();
        if (startMicOnStartup)
        {
            RestartMicrophoneListener();
            StartMicrophoneListener();

            _audio = GetComponent<AudioSource>();
            _audio.clip = Microphone.Start(null, true, 10, 44100);
            _audio.loop = true;
            while (!(Microphone.GetPosition(null) > 0)) { }//실시간 음성
            _audio.Play();
            _samples = new float[QSamples];
            _spectrum = new float[QSamples];
            _fSample = AudioSettings.outputSampleRate;
        }
    }

    private void Update()
    {
        if (stopMicrophoneListener)
        {
            StopMicroPhoneListener();
        }
        if (startMicrophoneListener)
        {
            StartMicrophoneListener();
        }

        MicroPhoneIntoAudioSource(microphoneListenerOn);
        DisableSound(!disableOutputSound);

        loudness = GetAveragedVolume() * sensitivity;
        GetPitch();

        //위를 바탕으로 애니메이션하는건 내가 알아서 해야됨 
        if(loudness > 1.5f)
        {
            eyecolor.material.color = Color.green;
            //Debug.Log("소리인식중");
        }
        else
        {
            eyecolor.material.color = Color.white;
        }

       // FindObjectOfType<Game>
    }
    float GetAveragedVolume()
    {
        float[] data = new float[256];
        float a = 0;
        _audio.GetOutputData(data, 0);
        foreach(float s in data)
        {
            a += Mathf.Abs(s);
        }
        return a / 256;
    }
    void GetPitch()
    {
        GetComponent<AudioSource>().GetOutputData(_samples, 0);
        float sum = 0;
        for(int i=0; i<QSamples; i++)
        {
            sum += _samples[i] * _samples[i];
        }
        RmsValue = Mathf.Sqrt(sum / QSamples);
        DbValue = 20 * Mathf.Log10(RmsValue / RefValue);
        if (DbValue < -160)
            DbValue = -160;

        GetComponent<AudioSource>().GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);
        float maxV = 0;
        var maxN = 0;
        for(int i=0; i<QSamples; i++)
        {
            if (!(_spectrum[i] > maxV) || !(_spectrum[i] > Threshold))
                continue;
            maxV = _spectrum[i];
            maxN = i;
        }

        float freqN = maxN;
        if(maxN>0 && maxN < QSamples-1)
        {
            var dL = _spectrum[maxN - 1] / _spectrum[maxN];
            var dR = _spectrum[maxN + 1] / _spectrum[maxN];
            freqN += 0.5f * (dR * dR - dL * dL);
        }

        PitchValue = freqN * (_fSample / 2) / QSamples;
    }

    void StopMicroPhoneListener()
    {
        microphoneListenerOn = false;
        disableOutputSound = false;
        src.Stop();
        src.clip = null;

        Microphone.End(null);
    }

    void StartMicrophoneListener()
    {
        microphoneListenerOn = true;
        disableOutputSound = true;
        RestartMicrophoneListener();
    }

    void DisableSound(bool SoundOn)
    {
        float volume = 0;
        if (SoundOn)
        {
            volume = 0.0f;
        }
        else
        {
            volume = -80.0f;
        }

        masterMixer.SetFloat("Master", volume);
    }
    void RestartMicrophoneListener()
    {
        src = GetComponent<AudioSource>();
        src.clip = null;
        timesinceRestart = Time.time;
    }

    void MicroPhoneIntoAudioSource(bool MicrophoneListenerOn)
    {
        if(microphoneListenerOn)
        {
            if(Time.time - timesinceRestart > 0.5f && !Microphone.IsRecording(null))
            {
                src.clip = Microphone.Start(null, true, 10, 44100);
                while (!(Microphone.GetPosition(null) > 0)) { }
                src.Play();
            }
        }
    }
}
