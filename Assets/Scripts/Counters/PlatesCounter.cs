using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlatesCounter : BaseCounter
{
    public event EventHandler OnPlateSpawn;
    public event EventHandler OnPlateRemoved;

    [SerializeField] private KitchenObjectSO plateKitchenObjectSO;

    private float spawnPlateTimer;
    private float spawnPlateTimerMax = 4f;
    private int platesSpawnedAmount;
    private int platesSpawnedAmountMax = 4;

    

    private void Update()
    {
        if (!IsServer) //Apenas o servidor vai rodar esse update
        {
            return;
        }

        spawnPlateTimer += Time.deltaTime;
        if (spawnPlateTimer >= spawnPlateTimerMax)
        {
            spawnPlateTimer = 0f;

            if (KitchenGameManager.Instance.IsGamePlaying() && platesSpawnedAmount < platesSpawnedAmountMax)
            {
                SpawnPlateServerRpc();
            }
        }
    }

    [ServerRpc]
    private void SpawnPlateServerRpc()
    {
        SpawnPlateClientRpc();
    }

    [ClientRpc]
    private void SpawnPlateClientRpc()
    {
        platesSpawnedAmount++;

        OnPlateSpawn?.Invoke(this, EventArgs.Empty);
    }


    public override void Interact(Player player)
    {
        if (!player.HasKitchenObject())
        {
            //Player nao esta carregando nada
            if (platesSpawnedAmount > 0)
            {
                //Tem pelo menos um prato disponivel           
                KitchenObject.SpawnKitchenObject(plateKitchenObjectSO, player);

                InteractLogicServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc()
    {
        InteractLogicClientRpc();
    }

    [ClientRpc]
    private void InteractLogicClientRpc()
    {
        platesSpawnedAmount--;

        OnPlateRemoved?.Invoke(this, EventArgs.Empty);
    }
}
