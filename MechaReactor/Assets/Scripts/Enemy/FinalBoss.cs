﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalBoss : MonoBehaviour
{
    public AbsorbClass Absorb;
    public BulletHellClass Bullet;
    public LaserClass Laser;
    public MissilesClass Missile;

    public float attackIntervals;
 
    public Animator anim;
    public AudioSource angryRoar;

    private EnemyController enemyController;

    private float originalMS;

    private float lastAttack;

    private bool isAngry = false;
    private bool isAttacking = false;
    private Attacks currentAttack;

    // ABSORPTION PARAMETERS
    [System.Serializable]
    public class AbsorbClass 
    {
        public GameObject absorbEffect;
        public float absorbingTime = 2.0f;
        public float waveSpeed = 8f;
        public Transform waveReleasePoint;
        public GameObject wavePrefab;
        public AudioSource absorbSFX;
        public AudioSource releaseSFX;
        [HideInInspector]
        public float absorbedDmg = 0.0f;
        [HideInInspector]
        public  bool isAbsorbing = false;    
    }

    // BULLET HELL PARAMETERS
    [System.Serializable]
    public class BulletHellClass 
    {
        public AudioSource shootSFX;

        public float attackSpeed = 0.8f;
        public float bulletDamage = 10.0f;
        public float bulletForce = 10.0f;

        public float bulletSize = 2.0f;

        public int maxBullets = 5;

        public int timesToFire = 10;

        public Transform[] bulletSpawnLocations;
        public GameObject bulletPrefab;
    }

    // LASER PARAMETERS
    [System.Serializable]
    public class LaserClass 
    {
        public float laserPower;
        public float chargeDuration;
        public int flashesToAttack;

        public AudioSource ChargeSource;
        public AudioSource FireSource;
        public AudioSource lockedOnSource;

        public Material laserMat;
        public LineRenderer laser;
        public CapsuleCollider2D capsule;
        [HideInInspector]
        public Vector2 finalPosition;
        [HideInInspector]
        public Vector3 finalDirectionToPlayer;
        [HideInInspector]
        public LayerMask mask;
        [HideInInspector]
        public float laserWidth = 0.25f;
        [HideInInspector]
        public bool isCharging = false;
        [HideInInspector]
        public bool hasFired = false;
    }

    // MISSILE PARAMETERS
    [System.Serializable]
    public class MissilesClass
    {
        public float missileImpactDamage = 20f;
        public float missileSpeed = 15f;
        public float missileFireRate = 0.3f;
        public float missileSpread = 6f;
        public int missilesToSpawnMin = 2;
        public int missilesToSpawnMax = 5;
        public GameObject missilePrefab;

        public AudioSource missileLaunchSource;

        public Transform[] missileFirePoints;
    }

    

    void Start()
    {
        enemyController = GetComponent<EnemyController>();
        originalMS = GetComponent<EnemyController>().movementSpeed;
        currentAttack = Attacks.NoAttack;
        Laser.mask = LayerMask.GetMask("Player") | LayerMask.GetMask("Obstacles");
    }

    void Update()
    {
        if(enemyController.currentHealth <= 0.0f)
        {
            cinematicDeath();
            return; 
        }

        if(enemyController.currentHealth < enemyController.maxHealth / 3 && !isAngry)
        {
            isAngry = true;
            activateAngerBuff();
        }

        //WE LOVE THE LASER ATTACK :)
        if(Laser.isCharging)
        {
            drawLaser();
        }

        if(Laser.hasFired)
        {
            Laser.laserWidth -= Time.deltaTime*2;
            Laser.laser.startWidth = Mathf.Clamp(Laser.laserWidth, 0.0f, 1.0f);
            float capsuleWidth = Laser.capsule.size.x;
            capsuleWidth -= Time.deltaTime*2;
            Laser.capsule.size = new Vector2(Mathf.Clamp(capsuleWidth, 0.0f, 1.0f), Laser.capsule.size.y);
        }
            
        if(isAttacking && !isAngry)
        {
            enemyController.movementSpeed = 0.0f;
        } 
        else
        {
            enemyController.movementSpeed = originalMS;
        }
    
        anim.SetFloat("movementSpeed", enemyController.movementSpeed);
        anim.SetBool("isAngry", isAngry);

        if(!isAttacking && Time.time > attackIntervals + lastAttack)
        {
            setAttack();
            executeAttack();
            if(isAngry)
            {
                Invoke("setAttack", 1.0f);
                Invoke("executeAttack", 1.1f);
            }
        }
    }

    public void activateAngerBuff()
    {
        angryRoar.Play();
        Bullet.attackSpeed -= 0.15f;
        Bullet.timesToFire += 2;
        Bullet.maxBullets += 1;
        Bullet.bulletSize = 1.5f;
        Laser.laserPower += 20;
        Laser.flashesToAttack = 3;
        Laser.chargeDuration = 1.9f;
        Absorb.waveSpeed += 3;
        Missile.missileSpread += 6f;
        Missile.missilesToSpawnMin += 2;
        Missile.missileFireRate -= 0.1f;

        attackIntervals = 1.8f;

        originalMS += 0.6f;
    }
    
    public void ProcessDamage(float dmg)
    {
        // If we're not in the process of absorbing, take damage like normal
        if(!Absorb.isAbsorbing)
            GetComponent<EnemyController>().TakeDamage(dmg);
        else   //Otherwise add the damage to the amount absorbed and take a sixth of the damage
        {
            Absorb.absorbedDmg += dmg;
            GetComponent<EnemyController>().TakeDamage(dmg/6);
        }   
    }

    private void setAttack()
    {
        while(true)
        {
            float roll = Random.Range(0f, 1f);
            if(roll < 0.25f && currentAttack != Attacks.Laser)
            {
                currentAttack = Attacks.Laser;
                break;
            }
            else if(roll >= 0.25f && roll < 0.50f && currentAttack != Attacks.Absorb)
            {
                currentAttack = Attacks.Absorb;
                break;
            }
            else if(roll >= 0.50f && roll < 0.75f && currentAttack != Attacks.BulletHell)
            {
                currentAttack = Attacks.BulletHell;
                break;
            }
            else if(roll >= 0.75f && roll < 1f && currentAttack != Attacks.Missiles)
            {
                currentAttack = Attacks.Missiles;
                break;
            }
        }
    }

    private void executeAttack()
    {
        isAttacking = true;
        switch(currentAttack)
        {
            case Attacks.Laser:
                laserAttack();
                break;
            case Attacks.Absorb:
                absorbAttack();
                break;
            case Attacks.BulletHell:
                bulletHellAttack();
                break;
            case Attacks.Missiles:
                missileAttack();
                break;
        }
    }

    private void cinematicDeath()
    {
        return;
    }

    /*
        ***************************************************
        Missile Attack
        ***************************************************
    */

    private void missileAttack()
    {
        StartCoroutine(BeginMissileAttack());
    }

    IEnumerator BeginMissileAttack()
    {
        yield return new WaitForSeconds(0.1f);
        int missilesToSpawn = Random.Range(Missile.missilesToSpawnMin, Missile.missilesToSpawnMax);
        for(int i = 0; i < missilesToSpawn; ++i)
        {
            foreach(Transform t in Missile.missileFirePoints)
            {
                fireMissile(t);
                yield return new WaitForSeconds(Missile.missileFireRate);
            }
        }
        isAttacking = false;
        lastAttack = Time.time;
    }

    private void fireMissile(Transform source)
    {
        Missile.missileLaunchSource.Play();
        GameObject missile = Instantiate(Missile.missilePrefab, source.position, source.rotation);
        missile.GetComponent<EnemyMissile>().setDamage(Missile.missileImpactDamage);
        Rigidbody2D missilerigid = missile.GetComponent<Rigidbody2D>();
        missile.transform.Rotate(0, 0, Random.Range(-Missile.missileSpread, Missile.missileSpread));
        missilerigid.AddForce(missile.transform.up * Missile.missileSpeed, ForceMode2D.Impulse);
    }

    /*
        ***************************************************
        LASER ATTACK

        pain 👉😎👉
        ***************************************************
    */
        

    private void laserAttack()
    {
        StartCoroutine(BeginLaserCharge());
    }

    IEnumerator BeginLaserCharge()
    {
        
        Laser.laser.enabled = true;
        Laser.isCharging = true;
        Laser.ChargeSource.Play();
        yield return new WaitForSeconds(Laser.chargeDuration);
        Laser.ChargeSource.Stop();
        Laser.lockedOnSource.Play();
        Laser.isCharging = false;
        enemyController.canRotate = false;
        for(int i = 0; i < Laser.flashesToAttack; ++i)
        {
            yield return new WaitForSeconds(0.05f);
            Laser.laser.enabled = false;
            yield return new WaitForSeconds(0.05f);
            Laser.laser.enabled = true;
        }
        Laser.lockedOnSource.Stop();
        fireLaser();
        Laser.FireSource.Play();
        enemyController.canRotate = true;
        lastAttack = Time.time;
        yield return new WaitForSeconds(0.8f);
        Laser.capsule.enabled = false;
        Laser.hasFired = false;
        Laser.laserWidth = 0.25f;
        isAttacking = false;
    }

    private void drawLaser()
    {
        Laser.laserMat.color = new Color(1, 0, 0, 0.25f);
        Laser.laser.startWidth = Laser.laserWidth;
        Laser.laserWidth += Time.deltaTime/6;

        Vector3 directionToPlayer = GetComponent<EnemyController>().player.transform.position - transform.position;
        RaycastHit2D hit = Physics2D.Raycast(transform.position + directionToPlayer.normalized, directionToPlayer, 400, Laser.mask);
        Laser.laser.SetPosition(0, Laser.laser.transform.position);
        Laser.laser.SetPosition(1, hit.point);
        Laser.finalDirectionToPlayer = directionToPlayer;
        Laser.finalPosition = hit.point;
    }

    private void fireLaser()
    {
        Laser.hasFired = true;
        
        Vector3 directionToPlayer = Laser.finalDirectionToPlayer;
        RaycastHit2D hit = Physics2D.Raycast(transform.position + directionToPlayer.normalized, directionToPlayer, 400, Laser.mask);
        Laser.laser.SetPosition(1, hit.point);
        Laser.laserMat.color = new Color(1, 0, 0, 1f);
        Laser.laser.startWidth = 1.5f;

        Laser.capsule.transform.position = (hit.point + new Vector2(transform.position.x, transform.position.y)) / 2;
        Laser.capsule.size = new Vector2(0.6f, hit.distance + 3.0f);
        float angle = Mathf.Atan2(transform.position.y - hit.point.y, transform.position.x - hit.point.x) *180/Mathf.PI - 90f;
        Laser.capsule.transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
        Laser.capsule.enabled = true;
    }

    /*
        ***************************************************
        ABSORPTION ATTACK
        ***************************************************
    */

    private void absorbAttack()
    {
        StartCoroutine(BeginAbsorb());
    }

    IEnumerator BeginAbsorb()
    {
        Absorb.isAbsorbing = true;
        Absorb.absorbEffect.SetActive(true);
        Absorb.absorbSFX.Play();

        yield return new WaitForSeconds(Absorb.absorbingTime);

        Absorb.absorbSFX.Stop();
        lastAttack = Time.time;
        Absorb.absorbEffect.SetActive(false);
        Absorb.isAbsorbing = false;
        ReleaseWaveAttack(Absorb.absorbedDmg);
        isAttacking = false;
    }

    private void ReleaseWaveAttack(float totalDmg)
    {
        if(isAngry && totalDmg < 5.0f)
        {
            totalDmg += 5f;
        }
        if(totalDmg != 0.0f)
        {
            Absorb.releaseSFX.Play();
            GameObject wave = Instantiate(Absorb.wavePrefab, Absorb.waveReleasePoint.position, Absorb.waveReleasePoint.rotation);
            wave.GetComponent<EnemyWave>().setDamage(totalDmg / 3);
            Rigidbody2D bulletrigid = wave.GetComponent<Rigidbody2D>();
            bulletrigid.AddForce(Absorb.waveReleasePoint.up * Absorb.waveSpeed, ForceMode2D.Impulse);
            Absorb.absorbedDmg = 0.0f;
        }
    }

    /*
        ***************************************************
        BULLET HELL ATTACK
        ***************************************************
    */

    private void bulletHellAttack()
    {
        StartCoroutine(BulletHellBegin());
    }

    IEnumerator BulletHellBegin()
    {
        for(int i = 0; i < Bullet.timesToFire; ++i)
        {
            int amount = Random.Range(1, Bullet.maxBullets);
            sprayBullets(amount);
            Bullet.shootSFX.Play();
            yield return new WaitForSeconds(Bullet.attackSpeed);
        }
        isAttacking = false;
        lastAttack = Time.time;
    }

    private void sprayBullets(int amount)
    {
        foreach(Transform spawn in Bullet.bulletSpawnLocations)
        {
            for(int i = 0; i < amount; ++i)
            {
                GameObject b = Instantiate(Bullet.bulletPrefab, spawn.transform.position, spawn.rotation);
                b.GetComponent<EnemyBullet>().setBulletDamage(Bullet.bulletDamage);
                b.transform.localScale *= Bullet.bulletSize;
                Rigidbody2D bulletrigid = b.GetComponent<Rigidbody2D>();
                b.transform.Rotate(0, 0, Random.Range(-50, 50));
                bulletrigid.AddForce(b.transform.up * Bullet.bulletForce, ForceMode2D.Impulse);
            }
        }
    }

    public enum Attacks
    {
        NoAttack,
        Laser,
        Absorb,
        BulletHell,
        Missiles
    }
}
