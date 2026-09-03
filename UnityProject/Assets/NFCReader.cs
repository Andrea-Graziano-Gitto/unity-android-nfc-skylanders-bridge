using System;
using UnityEngine;
using TMPro;

public class NFCReader : MonoBehaviour
{
    [Header("UI TextMeshPro")]
    [SerializeField] private TextMeshProUGUI txtTagID;

    // Mostra la sequenza 0/1
    [SerializeField] private TextMeshProUGUI txtNFCPattern;


    [Header("Settings")]

    // Intervallo tra le verifiche
    [SerializeField] private float checkInterval = 0.1f;

    // Numero di letture per ogni ciclo
    [SerializeField] private int numeroLetture = 10;

    [Header("Pulsanti")]
    [SerializeField] private GameObject btnStart;
    [SerializeField] private GameObject btnStop;


    // --------------------------------------------------
    // NFC
    // --------------------------------------------------

    private AndroidJavaObject activity;
    private AndroidJavaObject nfcAdapter;

    // Tag Android attualmente rilevato
    private AndroidJavaObject currentTag;

    // Tecnologia NFC utilizzata per controllare la presenza
    private AndroidJavaObject currentTech;


    // --------------------------------------------------
    // CICLO 0/1
    // --------------------------------------------------

    private string nfcPattern = "";
    private int readingIndex = 0;

    private float checkTimer;


    // Ultimo ID rilevato
    private string lastTagID = "Nessuno";

    // Riferimento a GoogleFormSender
    public GoogleFormSender googleFormSender;

    // Stato di lettura
    private bool isRunning = false;


    // --------------------------------------------------
    // TECNOLOGIE NFC SUPPORTATE
    // --------------------------------------------------

    // L'ordine non è particolarmente importante.
    // Proviamo tutte le tecnologie che Android ci comunica
    // per il Tag.
    private readonly string[] supportedTechnologies =
    {
        "android.nfc.tech.NfcA",
        "android.nfc.tech.NfcB",
        "android.nfc.tech.NfcF",
        "android.nfc.tech.NfcV",
        "android.nfc.tech.IsoDep",
        "android.nfc.tech.Ndef",
        "android.nfc.tech.NdefFormatable",
        "android.nfc.tech.MifareClassic",
        "android.nfc.tech.MifareUltralight"
    };


    // ==================================================
    // START
    // ==================================================

    void Start()
    {
        // Sicurezza valori Inspector
        if (numeroLetture < 1)
        {
            numeroLetture = 1;
        }

        if (checkInterval <= 0)
        {
            checkInterval = 0.1f;
        }




        // Reset
        nfcPattern = "";
        readingIndex = 0;
        lastTagID = "Nessuno";

        currentTag = null;
        currentTech = null;

        isRunning = false;
        UpdateButtonsState();


        // UI
        SetText("Nessuno");

        if (txtNFCPattern != null)
        {
            txtNFCPattern.text = "";
        }


        // ==================================================
        // ANDROID NFC
        // ==================================================

        if (Application.platform == RuntimePlatform.Android)
        {
            try
            {
                // Unity Activity
                using (AndroidJavaClass player =
                       new AndroidJavaClass(
                           "com.unity3d.player.UnityPlayer"))
                {
                    activity =
                        player.GetStatic<AndroidJavaObject>(
                            "currentActivity");
                }


                // NFC Adapter
                using (AndroidJavaClass adapterClass =
                       new AndroidJavaClass(
                           "android.nfc.NfcAdapter"))
                {
                    nfcAdapter =
                        adapterClass.CallStatic<AndroidJavaObject>(
                            "getDefaultAdapter",
                            activity);
                }


                // Controlli NFC
                if (nfcAdapter == null)
                {
                    SetText("NFC Non Supportato");
                    return;
                }


                if (!nfcAdapter.Call<bool>("isEnabled"))
                {
                    SetText("NFC Disattivato su Android");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "Errore inizializzazione NFC: " +
                    ex.Message);
            }
        }
    }


    // ==================================================
    // APPLICATION FOCUS
    // ==================================================

    void OnApplicationFocus(bool hasFocus)
    {
        if (Application.platform != RuntimePlatform.Android ||
            nfcAdapter == null)
        {
            return;
        }


        if (hasFocus)
        {
            EnableForegroundDispatch();
        }
        else
        {
            DisableForegroundDispatch();
        }
    }


    // ==================================================
    // ENABLE FOREGROUND DISPATCH
    // ==================================================

    private void EnableForegroundDispatch()
    {
        try
        {
            AndroidJavaObject intent =
                new AndroidJavaObject(
                    "android.content.Intent",
                    activity,
                    activity.Call<AndroidJavaObject>(
                        "getClass")
                );


            // FLAG_ACTIVITY_SINGLE_TOP
            intent.Call<AndroidJavaObject>(
                "addFlags",
                536870912
            );


            AndroidJavaClass pendingIntentClass =
                new AndroidJavaClass(
                    "android.app.PendingIntent");


            // FLAG_UPDATE_CURRENT
            int flags = 134217728;


            // Android 12+
            using (AndroidJavaClass buildVersion =
                   new AndroidJavaClass(
                       "android.os.Build$VERSION"))
            {
                int sdkInt =
                    buildVersion.GetStatic<int>("SDK_INT");


                if (sdkInt >= 31)
                {
                    // FLAG_MUTABLE
                    flags |= 33554432;
                }
            }


            AndroidJavaObject pendingIntent =
                pendingIntentClass.CallStatic<AndroidJavaObject>(
                    "getActivity",
                    activity,
                    0,
                    intent,
                    flags
                );


            nfcAdapter.Call(
                "enableForegroundDispatch",
                activity,
                pendingIntent,
                null,
                null
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Errore EnableForegroundDispatch: " +
                ex.Message);
        }
    }


    // ==================================================
    // DISABLE FOREGROUND DISPATCH
    // ==================================================

    private void DisableForegroundDispatch()
    {
        try
        {
            nfcAdapter.Call(
                "disableForegroundDispatch",
                activity
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Errore DisableForegroundDispatch: " +
                ex.Message);
        }
    }


    // ==================================================
    // UPDATE
    // ==================================================

    void Update()
    {
        if (Application.platform != RuntimePlatform.Android ||
            nfcAdapter == null)
        {
            return;
        }

        // Se la lettura non è attiva, non fare nulla
        if (!isRunning)
        {
            return;
        }


        checkTimer += Time.deltaTime;


        if (checkTimer < checkInterval)
        {
            return;
        }


        checkTimer = 0f;


        // ==================================================
        // CONTROLLO NFC
        // ==================================================

        bool tagPresent = CheckTagPresence();


        // ==================================================
        // SALVA 1 O 0
        // ==================================================

        SaveReading(tagPresent);


        // ==================================================
        // CICLO COMPLETATO
        // ==================================================

        if (readingIndex >= numeroLetture)
        {
            ProcessReadings();
        }
    }


    // ==================================================
    // LETTURA DEL NUOVO TAG
    // ==================================================

    private void ReadNewTagFromIntent()
    {
        try
        {
            AndroidJavaObject intent =
                activity.Call<AndroidJavaObject>(
                    "getIntent");


            if (intent == null)
            {
                return;
            }


            string action =
                intent.Call<string>("getAction");


            bool isTagDetected =
                "android.nfc.action.NDEF_DISCOVERED".Equals(action) ||
                "android.nfc.action.TAG_DISCOVERED".Equals(action) ||
                "android.nfc.action.TECH_DISCOVERED".Equals(action);


            if (!isTagDetected)
            {
                return;
            }


            // ==================================================
            // PRENDI IL TAG ANDROID
            // ==================================================

            AndroidJavaObject tag =
                intent.Call<AndroidJavaObject>(
                    "getParcelableExtra",
                    "android.nfc.extra.TAG");


            if (tag == null)
            {
                return;
            }


            // Salva il Tag
            currentTag = tag;


            // ==================================================
            // PRENDI ID
            // ==================================================

            byte[] rawId =
                tag.Call<byte[]>("getId");


            if (rawId != null &&
                rawId.Length > 0)
            {
                string tagId =
                    BitConverter
                        .ToString(rawId)
                        .Replace("-", ":");


                lastTagID = tagId;


                Debug.Log(
                    "NUOVO TAG: " +
                    lastTagID);
            }


            // ==================================================
            // CERCA UNA TECNOLOGIA UTILIZZABILE
            // ==================================================

            FindWorkingTechnology();


            // ==================================================
            // RESET INTENT
            // ==================================================

            AndroidJavaObject newIntent =
                new AndroidJavaObject(
                    "android.content.Intent",
                    activity,
                    activity.Call<AndroidJavaObject>(
                        "getClass")
                );


            activity.Call(
                "setIntent",
                newIntent
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Errore lettura nuovo Tag: " +
                ex.Message);
        }
    }


    // ==================================================
    // CERCA TECNOLOGIA NFC
    // ==================================================

    private void FindWorkingTechnology()
    {
        if (currentTag == null)
        {
            return;
        }


        CloseCurrentTechnology();


        foreach (string techName in supportedTechnologies)
        {
            try
            {
                using (AndroidJavaClass techClass =
                       new AndroidJavaClass(techName))
                {
                    AndroidJavaObject tech =
                        techClass.CallStatic<AndroidJavaObject>(
                            "get",
                            currentTag);


                    if (tech != null)
                    {
                        currentTech = tech;

                        Debug.Log(
                            "Tecnologia NFC trovata: " +
                            techName);

                        return;
                    }
                }
            }
            catch
            {
                // Questa tecnologia non è supportata
                // dal Tag. Proviamo la successiva.
            }
        }


        Debug.LogWarning(
            "Nessuna tecnologia NFC compatibile trovata.");
    }


    // ==================================================
    // CONTROLLO PRESENZA TAG
    // ==================================================

    private bool CheckTagPresence()
    {
        // --------------------------------------------------
        // Prima controlliamo se Android ci ha appena
        // consegnato un nuovo Tag.
        // --------------------------------------------------

        ReadNewTagFromIntent();


        // --------------------------------------------------
        // Se non abbiamo un Tag conosciuto
        // --------------------------------------------------

        if (currentTag == null)
        {
            return false;
        }


        // --------------------------------------------------
        // Se non abbiamo trovato una tecnologia,
        // proviamo nuovamente.
        // --------------------------------------------------

        if (currentTech == null)
        {
            FindWorkingTechnology();
        }


        if (currentTech == null)
        {
            return false;
        }


        // --------------------------------------------------
        // PROVA A CONNETTERSI AL TAG
        // --------------------------------------------------

        try
        {
            bool alreadyConnected =
                currentTech.Call<bool>("isConnected");


            if (!alreadyConnected)
            {
                currentTech.Call("connect");
            }


            bool connected =
                currentTech.Call<bool>("isConnected");


            if (connected)
            {
                // Il tag è fisicamente raggiungibile.

                // Chiudiamo la connessione dopo il test.
                // Alla prossima verifica la riapriremo.
                try
                {
                    currentTech.Call("close");
                }
                catch
                {
                }


                return true;
            }
        }
        catch (Exception)
        {
            // Connessione fallita.
            // Normalmente significa che il Tag
            // non è più raggiungibile.
        }


        // --------------------------------------------------
        // TAG NON PIÙ PRESENTE
        // --------------------------------------------------

        CloseCurrentTechnology();

        currentTag = null;

        return false;
    }


    // ==================================================
    // SALVA LETTURA 0 / 1
    // ==================================================

    private void SaveReading(bool tagDetected)
    {
        if (readingIndex >= numeroLetture)
        {
            return;
        }


        // 1 = tag presente
        // 0 = tag assente

        nfcPattern += tagDetected ? "1" : "0";


        readingIndex++;


        // ==================================================
        // AGGIORNA TMP IMMEDIATAMENTE
        // ==================================================

        if (txtNFCPattern != null)
        {
            txtNFCPattern.text = nfcPattern;
        }


        Debug.Log(
            "Lettura " +
            readingIndex +
            "/" +
            numeroLetture +
            " = " +
            (tagDetected ? "1" : "0") +
            " | Pattern = " +
            nfcPattern);
    }


    // ==================================================
    // PROCESSA CICLO
    // ==================================================

    private void ProcessReadings()
    {
        Debug.Log(
            "CICLO COMPLETATO: " +
            nfcPattern);


        // ==================================================
        // CONTROLLA SE SONO TUTTI 0
        // ==================================================

        bool tuttiZero = true;


        for (int i = 0;
             i < nfcPattern.Length;
             i++)
        {
            if (nfcPattern[i] == '1')
            {
                tuttiZero = false;
                break;
            }
        }


        // ==================================================
        // RISULTATO
        // ==================================================

        if (tuttiZero)
        {
            // Esempio:
            // 0000000000

            SetText("Nessuno");

            Debug.Log(
                "Tutte le letture sono 0 -> Nessuno");
        }
        else
        {
            // Esempio:
            // 1111111111
            // oppure
            // 0011111000

            SetText(lastTagID);

            Debug.Log(
                "Tag presente -> " +
                lastTagID);
        }


        // ==================================================
        // INVIA AL FORM
        // ==================================================

        if (googleFormSender != null)
        {
            googleFormSender.InviaTestoAlForm();
        }


        // ==================================================
        // RESET CICLO
        // ==================================================

        readingIndex = 0;
        nfcPattern = "";


        if (txtNFCPattern != null)
        {
            txtNFCPattern.text = "";
        }
    }


    // ==================================================
    // CHIUDE TECNOLOGIA
    // ==================================================

    private void CloseCurrentTechnology()
    {
        if (currentTech == null)
        {
            return;
        }


        try
        {
            bool connected =
                currentTech.Call<bool>("isConnected");


            if (connected)
            {
                currentTech.Call("close");
            }
        }
        catch
        {
        }


        currentTech = null;
    }


    // ==================================================
    // SET TEXT
    // ==================================================

    private void SetText(string text)
    {
        if (txtTagID != null)
        {
            txtTagID.text = text;
        }
    }


    // ==================================================
    // START READING
    // ==================================================

    public void StartReading()
    {
        isRunning = true;
        UpdateButtonsState();
        
        Debug.Log("[NFCReader] Lettura AVVIATA");
    }


    // ==================================================
    // STOP READING
    // ==================================================

    public void StopReading()
    {
        isRunning = false;
        UpdateButtonsState();

        // Reset valori
        readingIndex = 0;
        nfcPattern = "";

        SetText("Nessuno");

        if (txtNFCPattern != null)
        {
            txtNFCPattern.text = "";
        }

        Debug.Log("[NFCReader] Lettura FERMATA");
    }


    // ==================================================
    // UPDATE BUTTONS STATE
    // ==================================================

    private void UpdateButtonsState()
    {
        if (btnStart != null)
        {
            btnStart.SetActive(!isRunning);
        }

        if (btnStop != null)
        {
            btnStop.SetActive(isRunning);
        }
    }


    // ==================================================
    // DESTROY
    // ==================================================

    void OnDestroy()
    {
        CloseCurrentTechnology();


        if (Application.platform == RuntimePlatform.Android &&
            nfcAdapter != null)
        {
            try
            {
                DisableForegroundDispatch();
            }
            catch
            {
            }
        }
    }
}