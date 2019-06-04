using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public Text tablero;
    public Text wanted;
    public Text winner;
    public List<(string name, int kills, string weaponName, float life, bool alive)> deadOnes;
    public static UIManager Instance;

    void Start()
    {
        deadOnes = new List<(string, int, string, float, bool)>();
        Instance = this;
    }

    void Update()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy").Select(x => x.GetComponent<Enemy>());

        //////////////////////////
        //IA2-P1 (Operación 2) -> Tablero de posiciones
        /* A las utilizadas en la operación 1 se le suman Concat, Zip, Aggregate */
        tablero.text = enemies
        .Select(x => (x.name, x.kills, weaponName: x.weapon.name, x.life, alive: true))
        .Concat(deadOnes)
        .Where(x => x.kills > 0)
        .OrderByDescending(x => x.kills)
        .Take(5)
        .Zip(Enumerable.Range(1, int.MaxValue), (item, value) => (value, item))
        .Aggregate("", (acum, item) => acum += 
            "\n " + item.value + 
            " - " + item.item.name + 
            " - " + item.item.kills + 
            " / " + item.item.weaponName + 
            " - " + (item.item.alive ? item.item.life.ToString() : "DEAD"));
        //////////////////////////

        //////////////////////////
        //IA2-P1 (Operación 3) -> Personaje más buscado del juego (con mas enemigos que eligieron matarlo)
        var shootingEnemies = enemies.Where(x => x.shootingEnemy).Select(x => x.shootingEnemy);
        var mostWanted = shootingEnemies.Aggregate(new List<(Enemy, int)>(), (acum, current) => {

            bool alreadyContainsEnemy = false;
            acum.ForEach(x => {
                if (x.Item1 == current) alreadyContainsEnemy = true; 
            });

            if (!alreadyContainsEnemy) acum.Add((current, shootingEnemies.Where(x => x == current).Count()));

            return acum;
        }).OrderByDescending(x => x.Item2).FirstOrDefault();
        //////////////////////////

        if (mostWanted != default((Enemy, int))) wanted.text = "Most wanted: " + mostWanted.Item1.transform.name + " (" + mostWanted.Item2 + ")";

        if (enemies.Count() == 1)
        {
            winner.text = enemies.First().transform.name + " Wins!";
        }

    }
}
