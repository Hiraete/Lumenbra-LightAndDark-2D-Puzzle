using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFullController : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject bodyPrefab;
    public GameObject lightFormPrefab;
    public GameObject shadowFormPrefab;

    [Header("Form Settings")]
    public float formDuration = 10f;
    public KeyCode transformKey = KeyCode.Space;
    public KeyCode switchKey = KeyCode.Q;

    [Header("Body Settings")]
    public float bodyMaxHealth = 100f;
    public GameObject attackPrefab;
    public Transform attackPoint;
    public float attackCooldown = 0.5f;

    [Header("Light Form Settings")]
    public float lightMoveSpeed = 5f;
    public float blinkDistance = 5f;
    public float lightMaxEnergy = 100f;
    public float blinkCost = 20f;
    public float lightEnergyRegen = 10f;

    [Header("Shadow Form Settings")]
    public float shadowMoveSpeed = 5f;
    public float dashDistance = 5f;
    public float shadowMaxEnergy = 100f;
    public float dashCost = 20f;
    public float shadowEnergyRegen = 15f;

    [Header("Events")]
    public UnityEvent onDeath; // body öldüğünde

    [Header("Damage Settings")]
    public float damageOnWrongZone = 20f; // Body'ye hasar

    // --- Private ---
    private GameObject currentBody;
    private GameObject currentForm;
    private bool isInForm = false;
    private float formTimer = 0f;

    private PlayerRole currentRole = PlayerRole.Shadow; // İlk form Shadow
    private float attackTimer = 0f;

    private float lightCurrentEnergy;
    private float shadowCurrentEnergy;

    private float currentBodyHealth;

    private Rigidbody2D formRb;

    void Start()
    {
        SpawnBody(transform.position);
        lightCurrentEnergy = lightMaxEnergy;
        shadowCurrentEnergy = shadowMaxEnergy;
        currentBodyHealth = bodyMaxHealth;
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        // Body -> Form geçiş
        if (Input.GetKeyDown(transformKey))
        {
            if (!isInForm)
                EnterForm(currentRole);
            else
                ReturnToBody();
        }

        // Formdayken switch
        if (isInForm && Input.GetKeyDown(switchKey))
        {
            SwitchForm();
        }

        // Form süresi geri sayım
        if (isInForm)
        {
            formTimer -= Time.deltaTime;
            if (formTimer <= 0f) ReturnToBody();
        }

        // Body saldırısı
        if (!isInForm && Input.GetMouseButtonDown(0) && attackTimer <= 0f)
        {
            Attack();
            attackTimer = attackCooldown;
        }

        // Form hareket + energy
        if (isInForm && formRb != null)
        {
            Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

            if (currentRole == PlayerRole.Light)
            {
                formRb.MovePosition(formRb.position + moveInput * lightMoveSpeed * Time.deltaTime);

                if (Input.GetKeyDown(KeyCode.LeftShift) && lightCurrentEnergy >= blinkCost)
                {
                    Vector2 dir = moveInput != Vector2.zero ? moveInput : Vector2.right;
                    formRb.position += dir * blinkDistance;
                    lightCurrentEnergy -= blinkCost;
                }

                lightCurrentEnergy += lightEnergyRegen * Time.deltaTime;
                lightCurrentEnergy = Mathf.Clamp(lightCurrentEnergy, 0f, lightMaxEnergy);
            }
            else // Shadow
            {
                formRb.MovePosition(formRb.position + moveInput * shadowMoveSpeed * Time.deltaTime);

                if (Input.GetKeyDown(KeyCode.LeftShift) && shadowCurrentEnergy >= dashCost)
                {
                    Vector2 dir = moveInput != Vector2.zero ? moveInput : Vector2.right;
                    formRb.position += dir * dashDistance;
                    shadowCurrentEnergy -= dashCost;
                }

                shadowCurrentEnergy += shadowEnergyRegen * Time.deltaTime;
                shadowCurrentEnergy = Mathf.Clamp(shadowCurrentEnergy, 0f, shadowMaxEnergy);
            }
        }
    }

    void SpawnBody(Vector3 pos)
    {
        currentBody = Instantiate(bodyPrefab, pos, Quaternion.identity);
    }

    void EnterForm(PlayerRole role)
    {
        isInForm = true;
        formTimer = formDuration;
        currentRole = role;

        Vector3 spawnPos = currentBody.transform.position;

        if (role == PlayerRole.Light)
            currentForm = Instantiate(lightFormPrefab, spawnPos, Quaternion.identity);
        else
            currentForm = Instantiate(shadowFormPrefab, spawnPos, Quaternion.identity);

        formRb = currentForm.GetComponent<Rigidbody2D>();
        if (formRb == null) formRb = currentForm.AddComponent<Rigidbody2D>();
        formRb.gravityScale = 0;
    }

    void SwitchForm()
    {
        if (currentForm != null)
        {
            Vector3 pos = currentForm.transform.position;
            Destroy(currentForm);

            currentRole = currentRole == PlayerRole.Shadow ? PlayerRole.Light : PlayerRole.Shadow;
            EnterForm(currentRole);
            currentForm.transform.position = pos;
        }
    }

    public void ReturnToBody(bool tookDamage = false)
    {
        if (tookDamage)
        {
            currentBodyHealth -= damageOnWrongZone;
            if (currentBodyHealth <= 0f)
            {
                currentBodyHealth = 0f;
                onDeath?.Invoke();
            }
        }

        if (currentForm != null)
            Destroy(currentForm);

        isInForm = false;
    }

    void Attack()
    {
        if (attackPrefab != null && attackPoint != null)
            Instantiate(attackPrefab, attackPoint.position, attackPoint.rotation);
    }

    // Energy getter
    public float GetLightEnergyPercent() => lightCurrentEnergy / lightMaxEnergy;
    public float GetShadowEnergyPercent() => shadowCurrentEnergy / shadowMaxEnergy;
    public float GetBodyHealthPercent() => currentBodyHealth / bodyMaxHealth;

    // Role getter
    public PlayerRole GetCurrentRole() => currentRole;

    // --- Alan tetikleyicilerinden çağırılacak ---
    public void OnEnterWrongZone()
    {
        if (isInForm)
        {
            ReturnToBody(true);
        }
    }
}

public enum PlayerRole { Light, Shadow }
