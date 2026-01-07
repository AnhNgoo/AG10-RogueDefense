using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UILab : MonoBehaviour
{

    [SerializeField] private float countDownTime = 5.0f;
    [SerializeField] public TextMeshProUGUI countDownText;
    [SerializeField] public GameObject pausePanel;

    // Start is called before the first frame update
    void Start()
    {
        pausePanel.SetActive(false);
        countDownText.gameObject.SetActive(false);
    }

    public void Pause_Btn()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f; // Pause the game
    }

    public void Resume_Btn()
    {
        pausePanel.SetActive(false);
        StartCoroutine(ResumeCountDownCoroutine());
    }

    IEnumerator ResumeCountDownCoroutine()
    {
        countDownText.gameObject.SetActive(true);
        float currentTime = countDownTime;
        while (currentTime > 0)
        {
            countDownText.text = Mathf.Ceil(currentTime).ToString();
            yield return new WaitForSecondsRealtime(1.0f);
            currentTime -= 1.0f;
        }
        countDownText.text = "Go!";
        yield return new WaitForSecondsRealtime(1.0f);
        countDownText.gameObject.SetActive(false);
        Time.timeScale = 1f; // Resume the game
    }
}
