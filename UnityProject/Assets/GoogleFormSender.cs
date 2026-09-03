using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class GoogleFormSender : MonoBehaviour
{
    [Header("UI Componenti")]
    [SerializeField] private TextMeshProUGUI testoDaInviare;
    public TMP_InputField urlInputField;
    public TMP_InputField entryIdInputField;

    private const string FormUrlPrefKey = "GoogleFormUrl";
    private const string EntryIdPrefKey = "GoogleFormEntryId";
    
[Header("Configurazione Google Form")]
[Tooltip("Inserisci l'URL del form che termina con /formResponse")]
[SerializeField] private string formUrl = "";

[Tooltip("Inserisci l'ID del campo, es. entry.123456789")]
[SerializeField] private string entryID = "";

    private void Start()
    {
        string savedFormUrl = PlayerPrefs.GetString(FormUrlPrefKey, string.Empty);
        string savedEntryId = PlayerPrefs.GetString(EntryIdPrefKey, string.Empty);

        if (!string.IsNullOrEmpty(savedFormUrl))
        {
            formUrl = savedFormUrl;
            if (urlInputField != null)
            {
                urlInputField.text = formUrl;
            }
        }

        if (!string.IsNullOrEmpty(savedEntryId))
        {
            entryID = savedEntryId;
            if (entryIdInputField != null)
            {
                entryIdInputField.text = entryID;
            }
        }
    }

    public void sendUrl()
    {
        if (urlInputField == null)
        {
            Debug.LogError("[GoogleForm] Assegna urlInputField nell'Inspector!");
            return;
        }

        PlayerPrefs.SetString(FormUrlPrefKey, urlInputField.text);
        PlayerPrefs.Save();
        formUrl = urlInputField.text;
    }

    public void SendEntryId()
    {
        if (entryIdInputField == null)
        {
            Debug.LogError("[GoogleForm] Assegna entryIdInputField nell'Inspector!");
            return;
        }

        PlayerPrefs.SetString(EntryIdPrefKey, entryIdInputField.text);
        PlayerPrefs.Save();
        entryID = entryIdInputField.text;
    }

    // Funzione da collegare al pulsante o chiamare via codice
    public void InviaTestoAlForm()
    {
        if (testoDaInviare == null)
        {
            Debug.LogError("[GoogleForm] Assegna il TextMeshProUGUI nell'Inspector!");
            return;
        }

        StartCoroutine(PostToForm(testoDaInviare.text));
    }

    private IEnumerator PostToForm(string testo)
    {
        WWWForm form = new WWWForm();
        form.AddField(entryID, testo);

        using (UnityWebRequest www = UnityWebRequest.Post(formUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[GoogleForm] Inviato con successo: '{testo}'");
            }
            else
            {
                Debug.LogError($"[GoogleForm] Errore invio: {www.error}");
            }
        }
    }
}