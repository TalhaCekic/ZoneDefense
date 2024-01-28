using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RobotRush.Types;

namespace RobotRush
{
	/// <summary>
	/// This script displays items that can be unlocked with in-game money. Each item has a 3D icon that spins while showing the price of the item if it is still locked. Locked items are also darkened.
	/// </summary>
	public class RRGShop : MonoBehaviour 
	{
        // Hold the gamecontroller for quicker access
        internal RRGGameController gameController;

        //How much money we have left in the shop
        internal int moneyLeft = 0;

        [Tooltip("The text object that displays the money we have")]
        public Transform moneyText;

        [Tooltip("The text object that displays the price of the current item")]
        public Text priceText;
    
        [Tooltip("The sign that appears next to the money text")]
        public string moneySign = "$";

        [Tooltip("The player prefs record of the money we have")]
        public string moneyPlayerprefs = "Money";

        //The current item we are on. The first item is 0, the second 1, etc
        internal int currentItem = 0;

        [Tooltip("The player prefs record of the current item we selected")]
        public string currentPlayerprefs = "CurrentPlayer";
        internal RRGPlayer currentPlayer;

        [Tooltip("A list of items in the game, their names, the scene they link to, the price to unlock it, and the state of the item ( -1 - locked and can't be played, 0 - unlocked no stars, 1-X the star rating we got on the item")]
        public ShopItem[] items;
        
        // The current item icon, 2D or 3D
        internal Transform currentIcon;
        
        [Tooltip("The effect that appears when we unlock this item")]
        public Transform unlockEffect;

        [Tooltip("Buttons for going to the next/previous items, and selecting items")]
        public Button buttonNextItem;
        public Button buttonPrevItem;
        public Button buttonSelectItem;

        [Tooltip("Keyboard/Gamepad buttons for changing and selecting items")]
        public string changeItemButton = "Horizontal";
        public string selectItemButton = "Submit";
        internal bool buttonPressed = false;
        
        [Tooltip("The position of the currently selected item in 3D space")]
        public Vector3 itemIconPosition;

        [Tooltip("The rotation speed of the currently selected item in 3D space")]
        public float itemSpinSpeed = 100;
        internal float itemRotation = 0;

        public Texture lockedTexture;

        // Maximum values of stats for , Rotation speed, Health, and Damage
        internal float healthMax = 0;
        internal float damageMax = 0;
        internal float rotateSpeedMax = 0;

        internal int index;
        
        public void OnEnable()
        {
            DontDestroyOnLoad(this);

            // If we find another Shop script, remove this one
            if ( GameObject.FindObjectOfType<RRGShop>() && GameObject.FindObjectsOfType<RRGShop>().Length > 1 ) Destroy(GameObject.FindObjectOfType<RRGShop>().gameObject);

            if (items.Length <= 0 ) return;
            
            //Get the number of money we have
            moneyLeft = PlayerPrefs.GetInt(moneyPlayerprefs, moneyLeft);

            //Update the text of the money we have
            moneyText.GetComponent<Text>().text = moneyLeft.ToString() + moneySign;

            // If the max stats for speed rotation and health have not been set yet, set them based on the highest stats in all items
            if (rotateSpeedMax == 0 && healthMax == 0 && damageMax == 0)
            {
                for (index = 0; index < items.Length; index++)
                {
                    if (rotateSpeedMax < items[index].itemIcon.GetComponent<RRGPlayer>().rotateSpeed) rotateSpeedMax = items[index].itemIcon.GetComponent<RRGPlayer>().rotateSpeed;
                    if (healthMax < items[index].itemIcon.GetComponent<RRGPlayer>().health) healthMax = items[index].itemIcon.GetComponent<RRGPlayer>().health;
                    if (damageMax < items[index].itemIcon.GetComponent<RRGPlayer>().shotObject.shotDamage) damageMax = items[index].itemIcon.GetComponent<RRGPlayer>().shotObject.shotDamage;
                }
            }

            //Get the number of the current item
            currentItem = PlayerPrefs.GetInt(currentPlayerprefs, currentItem);

            currentPlayer = items[currentItem].itemIcon.GetComponent<RRGPlayer>();

            // Set the item
            if ( GetComponent<Canvas>().enabled ) ChangeItem(0);


        }

        public void OnDisable()
        {
            // Remove the icon of the previous item
            if (currentIcon)
            {
                // Reset the item rotation
                itemRotation = currentIcon.eulerAngles.y;

                Destroy(currentIcon.gameObject);
            }
        }

        void Start()
        {
            //PlayerPrefs.DeleteAll();

            // If there are no items in the shop, we can't use it
            if (items.Length <= 0) return;
            
            // Listen for a click to change to the next item
            buttonNextItem.onClick.AddListener(delegate { ChangeItem(1); });

            // Listen for a click to change to the next item
            buttonPrevItem.onClick.AddListener(delegate { ChangeItem(-1); });

            // Listen for a click to start the game in the current item
            buttonSelectItem.onClick.AddListener(delegate { StartCoroutine("SelectItem"); });
        }

        void Update()
        {
            // If there are no items in the shop, we can't use it
            if (items.Length <= 0 || GetComponent<Canvas>().enabled == false ) return;            

            // Rotate the current item
            if (currentIcon) currentIcon.Rotate(Vector3.up * itemSpinSpeed * Time.deltaTime, Space.World);

            // Keyboard and gamepad controls
            if ( buttonPressed == false )
            {
                if (Input.GetAxisRaw(changeItemButton) > 0) buttonNextItem.onClick.Invoke();// ChangeItem(1);
                if (Input.GetAxisRaw(changeItemButton) < 0) buttonPrevItem.onClick.Invoke();// ChangeItem(-1);
                if (Input.GetButtonDown(selectItemButton)) buttonSelectItem.onClick.Invoke();// SelectItem();

                if (Input.GetAxisRaw(changeItemButton) != 0 || Input.GetButton(selectItemButton)) buttonPressed = true;

            }
            else if (Input.GetAxisRaw(changeItemButton) == 0 || Input.GetButtonUp(selectItemButton)) buttonPressed = false;
        }

        /// <summary>
        /// Changes to the next or previous item and updates the new item based on lockstate
        /// </summary>
        /// <param name="changeValue"></param>
        public void ChangeItem( int changeValue )
        {
            // If there are no items in the shop, we can't use it
            if (items.Length <= 0) return;

            // Change the index of the item
            currentItem += changeValue;

            // Make sure we don't go out of the list of available items
            if (currentItem > items.Length - 1) currentItem = 0;
            else if (currentItem < 0) currentItem = items.Length - 1;

            // Remove the icon of the previous item
            if (currentIcon)
            {
                // Reset the item rotation
                itemRotation = currentIcon.eulerAngles.y;

                Destroy(currentIcon.gameObject);
            }

            // Display the icon of the current item
            if ( items[currentItem].itemIcon )
            {
                // Create the current item
                currentIcon = Instantiate(items[currentItem].itemIcon.transform, itemIconPosition, Quaternion.identity) as Transform;

                // Set the rotation of the item
                currentIcon.eulerAngles = Vector3.up * itemRotation;

                // If the item has an animation, play it
                if (currentIcon.GetComponent<Animation>()) currentIcon.GetComponent<Animation>().Play();

                // Set the stats of the current item, move speed, health, rotation speed, and damage
                if ( currentIcon.GetComponent<RRGPlayer>())
                {
                    // Fill values for the icons
                    if (transform.Find("Base/Stats/RotateSpeed/Full")) transform.Find("Base/Stats/RotateSpeed/Full").GetComponent<Image>().fillAmount = currentIcon.GetComponent<RRGPlayer>().rotateSpeed/rotateSpeedMax;
                    if (transform.Find("Base/Stats/Health/Full")) transform.Find("Base/Stats/Health/Full").GetComponent<Image>().fillAmount = currentIcon.GetComponent<RRGPlayer>().health/healthMax;
                    if (transform.Find("Base/Stats/Damage/Full")) transform.Find("Base/Stats/Damage/Full").GetComponent<Image>().fillAmount = currentIcon.GetComponent<RRGPlayer>().shotObject.shotDamage / damageMax;

                    // Text values inside the icons
                    if (transform.Find("Base/Stats/RotateSpeed/Text")) transform.Find("Base/Stats/RotateSpeed/Text").GetComponent<Text>().text = currentIcon.GetComponent<RRGPlayer>().rotateSpeed.ToString();
                    if (transform.Find("Base/Stats/Health/Text")) transform.Find("Base/Stats/Health/Text").GetComponent<Text>().text = currentIcon.GetComponent<RRGPlayer>().health.ToString();
                    if (transform.Find("Base/Stats/Damage/Text")) transform.Find("Base/Stats/Damage/Text").GetComponent<Text>().text = currentIcon.GetComponent<RRGPlayer>().shotObject.shotDamage.ToString();
                }
            }

            // Get the lock state of the current item
            items[currentItem].lockState = PlayerPrefs.GetInt(currentIcon.name, items[currentItem].lockState);

            // If the item is unlocked, show the "GO!" button
            if ( items[currentItem].lockState == 1 )
            {
                if (priceText) priceText.text = "GO!";

                // Set the currently selected item in the shop
                PlayerPrefs.SetInt(currentPlayerprefs, currentItem);
            }
            else // Otherwise, darken the item and show unlock price
            {
                // Get the renderer of the chasis ( main body ) of the car object
                MeshRenderer[] meshRenderers = currentIcon.GetComponentsInChildren<MeshRenderer>();
                
                // Go through all the materials of the renderer and darken them
                for ( index = 0; index < meshRenderers.Length; index++)
                {
                    //print(meshRenderers[index].transform.name);
                    //meshRenderers[index].material.SetColor("_Color", Color.black);
                    //meshRenderers[index].material.SetColor("_EmissionColor", Color.black);

                    //Material newMaterial = new Material(defaultMaterial);

                    meshRenderers[index].material.SetTexture("_MainTex", lockedTexture);
                }

                // Display the price of the current item
                if (priceText) priceText.text = items[currentItem].price.ToString() + moneySign.ToString();
            }
        }
        
        /// <summary>
        /// Select the current item. If it's locked and we have enough money we unlock it.
        /// If it is already unlocked we play with the selected item.
        /// </summary>
        public void SelectItem()
        {
            // If the item is unlocked, select it and start the game
            if (items[currentItem].lockState == 1)
            {
                currentPlayer = items[currentItem].itemIcon.GetComponent<RRGPlayer>();
                
                // Destroy the shop icon
                Destroy(currentIcon.gameObject);
                
                // Deactivate the shop object
                gameObject.GetComponent<Canvas>().enabled = false;
            }
            else if ( moneyLeft >= items[currentItem].price )
            {
                // Unlock the item
                items[currentItem].lockState = 1;

                // Set the lock state of the current item
                PlayerPrefs.SetInt(currentIcon.name, items[currentItem].lockState);

                // Deduct the price of the item from the money we have
                moneyLeft -= items[currentItem].price;
                
                // Save the money we have left
                PlayerPrefs.SetInt(moneyPlayerprefs, moneyLeft);

                //Update the text of the money we have
                moneyText.GetComponent<Text>().text = moneyLeft.ToString() + moneySign;

                // Create the unlock effect on the item
                if (unlockEffect) Instantiate(unlockEffect, currentIcon.position, currentIcon.rotation);

                // Set the item
                ChangeItem(0);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawCube(itemIconPosition, new Vector3(1,0.1f,1));
        }

        public void quitApp()
        {
            Application.Quit();
        }
    }
}