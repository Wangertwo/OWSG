using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        if (grasswalkSound != null)
        {
            grasswalkSound.loop = true;
            grasswalkSound.volume = 0.8f;
        }
    }

    public AudioSource dropItemSound;
    public AudioSource chopTreeSound;
    public AudioSource toolSwingSound;
    public AudioSource craftingSound;
    public AudioSource pickupItemSound;
    public AudioSource grasswalkSound;

    // Background music 
    public AudioSource backgroundMusic;

    private float lastToolSwingTime;
    private float lastChopTime;
    private const float SoundCooldown = 0.3f;

    public void PlayDropItemSound()
    {
        if (dropItemSound != null)
        {
            dropItemSound.Play();
        }
    }

    public void PlayChopItemSound()
    {
        if (chopTreeSound != null && Time.time - lastChopTime >= SoundCooldown)
        {
            lastChopTime = Time.time;
            chopTreeSound.Stop();
            chopTreeSound.Play();
            chopTreeSound.SetScheduledEndTime(AudioSettings.dspTime + 1.0);
        }
    }

    public void PlayToolSwingSound()
    {
        if (toolSwingSound != null && Time.time - lastToolSwingTime >= SoundCooldown)
        {
            lastToolSwingTime = Time.time;
            toolSwingSound.Stop();
            toolSwingSound.Play();
        }
    }

    public void PlayCraftingSound()
    {
        if (craftingSound != null)
        {
            craftingSound.Stop();
            craftingSound.Play();
            craftingSound.SetScheduledEndTime(AudioSettings.dspTime + 1.0);
        }
    }

    public void PlayPickupItemSound()
    {
        if (pickupItemSound != null)
        {
            pickupItemSound.Play();
        }
    }

    public void PlayGrassWalkSound()
    {
        if (grasswalkSound != null && !grasswalkSound.isPlaying)
        {
            grasswalkSound.Play();
        }
    }

    public void StopGrassWalkSound()
    {
        if (grasswalkSound != null && grasswalkSound.isPlaying)
        {
            grasswalkSound.Stop();
        }
    }
}
