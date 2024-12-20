using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class FloatingBar : MonoBehaviour
{
    [SerializeField] private Transform mainCam, unit, wsCanvas;

    [SerializeField] private Vector3 offset;

    private Slider slider;
    private EnemyHealth enemy;

    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main.gameObject.transform;


        slider = GetComponent<Slider>();
        if(GetComponentInParent<EnemyHealth>() != null)
        {
            enemy = GetComponentInParent<EnemyHealth>();
        }
        else
        {
            gameObject.SetActive(false);
        }
        transform.SetParent(wsCanvas);
        slider.maxValue = enemy.maxHealth;
        slider.value = enemy.GetCurrentHealth();
        enemy.DamageEvent.AddListener(UpdateBar);
        enemy.DeathEvent.AddListener(Death);

        
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
        transform.position = unit.position + offset;
    }

    private void UpdateBar()
    {
        slider.value = enemy.GetCurrentHealth();
    }

    private void Death()
    {
        gameObject.SetActive(false);
    }

}
