using UnityEngine;

/// <summary>Simple arena mini-boss: follows the player, pauses to telegraph,
/// then charges. It deliberately shares EnemyHealth rather than duplicating
/// the Gatekeeper architecture.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public class MedinaWarden : MonoBehaviour
{
    [SerializeField] private float noticeRange = 7f;
    [SerializeField] private float chargeRange = 3f;
    [SerializeField] private float walkSpeed = 1.8f;
    [SerializeField] private float chargeSpeed = 7f;
    [SerializeField] private float windup = .55f;
    [SerializeField] private float chargeDuration = .3f;
    [SerializeField] private float cooldown = 1.1f;
    private Rigidbody2D body;
    private EnemyHealth health;
    private Transform player;
    private float timer;
    private int chargeDirection;
    private enum State { Watch, Windup, Charge, Recover }
    private State state;
    void Awake()
    {
        body = GetComponent<Rigidbody2D>(); health = GetComponent<EnemyHealth>();
        PlayerHealth found = FindFirstObjectByType<PlayerHealth>(); if (found != null) player = found.transform;
    }
    void Update()
    {
        if (health.IsDead || health.IsStunned || player == null) return;
        timer -= Time.deltaTime;
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > noticeRange) { body.linearVelocity = new Vector2(0, body.linearVelocity.y); return; }
        if (state == State.Watch)
        {
            body.linearVelocity = new Vector2(Mathf.Sign(dx) * walkSpeed, body.linearVelocity.y);
            if (Mathf.Abs(dx) <= chargeRange && timer <= 0f) { state = State.Windup; timer = windup; chargeDirection = dx >= 0 ? 1 : -1; }
        }
        else if (state == State.Windup)
        {
            body.linearVelocity = new Vector2(0, body.linearVelocity.y);
            if (timer <= 0f) { state = State.Charge; timer = chargeDuration; }
        }
        else if (state == State.Charge)
        {
            body.linearVelocity = new Vector2(chargeDirection * chargeSpeed, body.linearVelocity.y);
            if (timer <= 0f) { state = State.Recover; timer = cooldown; }
        }
        else if (timer <= 0f) state = State.Watch;
    }
}
