using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IHealth
{
    private int currentHealth;
    public int maxHealth;
    private Animator anim;

    public UnityEvent takeDamageEvent;
    
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public void TakeDamage(int damage)
    {
        anim.SetTrigger("Take Hit");

        currentHealth -= damage;

        takeDamageEvent?.Invoke();
    }
}
