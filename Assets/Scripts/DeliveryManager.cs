
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour
{


    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;


    public static DeliveryManager Instance { get; private set; }


    [SerializeField] private RecipeListSO recipeListSO;
    private List<RecipeSO> waitingRecipeSOList;

    private float spawnRecipeTimer = 4f;
    private float spawnRecipeTimerMax = 4f;

    private int waitingRecipesMax = 4;

    private int successfulRecipesAmount;

    private void Awake()
    {
        Instance = this;
        waitingRecipeSOList = new List<RecipeSO>(); //Iniciando a lista
    }
    
    private void Update()
    {
        if (!IsServer) //Só roda no servidor
        {
            return;
        }

        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f)
        {
            spawnRecipeTimer = spawnRecipeTimerMax; //Resetando o tempo de spawn

            if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipesMax)
            {
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);                

                SpawnNewWaitingRecipeClientRpc(waitingRecipeSOIndex);                
            }
        }
    }

    [ClientRpc]
    private void SpawnNewWaitingRecipeClientRpc(int waitingRecipeSOIndex)
    {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];

        waitingRecipeSOList.Add(waitingRecipeSO);

        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }


    public void DeliveryRecipe(PlateKitchenObject plateKitchenObject)
    {
        for (int i = 0; i < waitingRecipeSOList.Count; i++)
        {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];

            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count)
            {
                //Mesmo numero de ingredientes
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList)
                {
                    //Vendo todos os ingredientes da receita
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList())
                    {
                        //Vendo todos os ingredientes do prato
                        if (plateKitchenObjectSO == recipeKitchenObjectSO)
                        {
                            //Os ingredientes são iguais
                            ingredientFound = true;
                            break;
                        }
                    }
                    if (!ingredientFound)
                    {
                        //O ingrediente da receita não foi encontrado no prato
                        plateContentsMatchesRecipe = false;
                    }
                }
                if (plateContentsMatchesRecipe)
                {
                    //Jogador entregou a receita correta
                    DeliveryCorrectRecipeServerRpc(i);
                    return;
                }
            }
        }
        //Nao foi encontrado os ingredientes corretos
        //Jogador entregou a receita errada
        DeliveryIncorrectRecipeServerRpc();
    }


    [ServerRpc(RequireOwnership = false)] //Dessa forma o client tambem consegue ativar o evento de entrega
    private void DeliveryIncorrectRecipeServerRpc()
    {
        DeliveryIncorrectRecipeClientRpc();
    }

    [ClientRpc]
    private void DeliveryIncorrectRecipeClientRpc()
    {
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }


    [ServerRpc(RequireOwnership = false)]
    private void DeliveryCorrectRecipeServerRpc(int waitingRecipeSOListIndex)
    {
        DeliveryCorrectRecipeClientRpc(waitingRecipeSOListIndex);
    }

    [ClientRpc]
    private void DeliveryCorrectRecipeClientRpc(int waitingRecipeSOListIndex)
    {
        successfulRecipesAmount++;
        waitingRecipeSOList.RemoveAt(waitingRecipeSOListIndex);

        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }



    public List<RecipeSO> GetWaitingRecipeSOList() //Forma de enviar a lista para outro script
    {
        return waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount()
    {
        return successfulRecipesAmount;
    }

}

