using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public GameObject heartPrefab;
    public Transform heartsParent;

    private List<Image> hearts = new List<Image>();

    void Start()
    {
        CreateHearts();
        UpdateHearts();
    }

    public void UpdateHearts()
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].enabled = i < playerHealth.CurrentHealth;
        }
    }

    void CreateHearts()
    {
        foreach (Transform child in heartsParent)
            Destroy(child.gameObject);

        hearts.Clear();

        for (int i = 0; i < playerHealth.maxHealth; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartsParent);
            hearts.Add(heart.GetComponent<Image>());
        }
    }
}
