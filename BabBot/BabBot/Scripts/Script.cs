﻿/*
    This file is part of BabBot.

    BabBot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BabBot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BabBot.  If not, see <http://www.gnu.org/licenses/>.
  
    Copyright 2009 BabBot Team
*/
//css_import Paladin;
using System;
using BabBot.Bot;
using BabBot.Scripting;
using BabBot.Wow;

namespace BabBot.Scripts
{
    public class Script : IScript
    {
        protected PlayerActionList Actions;
        protected BindingList Bindings;
        private IHost parent;
        public IPlayerWrapper player;

        #region Configurable properties

        protected int MinMPPct = 80; // Minimum mana percentage to start drinking
        protected int MinHPPct = 80; // Minimum health percentage to start eating
        protected float MinMeleeDistance = 1.0f;
        protected float MaxMeleeDistance = 5.0f;
        protected float MinRangedDistance = 15.0f;
        protected float MaxRangedDistance = 25.0f;
        protected int HpPctEmergency = 25; // Minimum health percentage at which we call the emergency healing routine
        protected int HpPctPotion = 20; // Minimum health percentage at which we look for a health potion
        protected int MpPctPotion = 15; // Minimum mana percentage at which we look for a mana potion

        #endregion

        #region Lists

        protected SpellList HealingSpells;

        #endregion

        #region IScript Members

        IHost IScript.Parent
        {
            set { parent = value; }
        }

        IPlayerWrapper IScript.Player
        {
            set { player = value; }
        }

        /// <summary>
        /// Local script initialization. Not much to do here at the moment
        /// </summary>
        void IScript.Init()
        {
            Console.WriteLine("Init() -- Begin");
            Bindings = new BindingList();
            Actions = new PlayerActionList();
            HealingSpells = new SpellList();
            Console.WriteLine("Init() -- End");
        }

        /// <summary>
        /// Called at every update of the player data from the main thread of the bot
        /// aka the main routine
        /// </summary>
        void IScript.Update()
        {
            Console.WriteLine("Update() -- Begin");
            Console.WriteLine("Current State: " + player.State());
            switch (player.State())
            {
                case PlayerState.PreMobSelection:
                    break;
                case PlayerState.PostMobSelection:
                    break;
                case PlayerState.Start:
                    OnStart();
                    break;
                case PlayerState.WayPointTimeout:
                    break;
                case PlayerState.PreRest:
                    break;
                case PlayerState.Rest:
                    OnRest();
                    break;
                case PlayerState.PostRest:
                    break;
                case PlayerState.Dead:
                    OnDead();
                    break;
                case PlayerState.Graveyard:
                    OnGraveyard();
                    break;
                case PlayerState.PreResurrection:
                    break;
                case PlayerState.PostResurrection:
                    break;
                case PlayerState.PreLoot:
                    break;
                case PlayerState.PostLoot:
                    break;
                case PlayerState.PreCombat:
                    OnPreCombat();
                    break;
                case PlayerState.InCombat:
                    OnInCombat();
                    break;
                case PlayerState.PostCombat:
                    OnPostCombat();
                    break;
                case PlayerState.Sale:
                    break;
                case PlayerState.Roaming:
                    OnRoaming();
                    break;
                case PlayerState.Stop:
                    OnStop();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.WriteLine("Update() -- End");
        }

        #endregion

        /// <summary>
        /// Called when we have attached ourselves to WoW's client or when we start botting.
        /// </summary>
        private void OnStart()
        {
            // No ideas for now :)
        }

        /// <summary>
        /// Called when we have detach from WoW's client or when we hit the stop button and stop botting.
        /// This state happens only once of course
        /// </summary>
        private void OnStop()
        {
            /// We stop movement just in case we're moving. Eventually we might want to stop fighting as well.
            player.Stop();
        }

        /// <summary>
        /// We are either a ghost or we just died
        /// </summary>
        private void OnDead()
        {
            /// We should run back to our corpse
        }

        /// <summary>
        /// We are near the graveyard (aka the spirit healer is in range)
        /// </summary>
        private void OnGraveyard()
        {
            /// If we managed to get to the graveyard and we have already run around
            /// like a crazy chicken without finding our own body, well.. it's time
            /// to resurrect at the spirit healer
        }

        /// <summary>
        /// We are roaming through the waypoints with nothing else to do
        /// </summary>
        private void OnRoaming()
        {
            /// This is where we should walk through the waypoints and 
            /// check what happens around us (like if there's anything to
            /// attack or anything attacking us, or if we run out of mana/health,
            /// or if we should rebuff something)
            /// 
            /// Right now we only walk through the waypoints as a proof of concept
            Console.WriteLine("OnRoaming() -- Walking to the next waypoint");
            player.WalkToNextWayPoint(WayPointType.Normal);
        }

        /// <summary>
        /// This happens when we are being attacked by some mobs or when we
        /// have found something to kill 
        /// </summary>
        private void OnPreCombat()
        {
            Console.WriteLine("OnPreCombat()");
            if (player.IsBeingAttacked())
            {
                Console.WriteLine("OnPreCombat() - We are being attacked");
                /// We are being attacked by a Mob. That means that we should fight back
                /// by finding the mob first of all
                if (player.SelectWhoIsAttackingUs())
                {
                    /// We found who is attacking us and we fight back (no rebuffing now)
                    /// (If everything is correct at this point the StateManager will take care
                    /// of switching to the OnCombat state)
                }
            }
            else
            {
                Console.WriteLine("OnPreCombat() - We are going to attack someone");
                if (player.EnemyInSight())
                {
                    // Face the closest enemy
                    Console.WriteLine("OnPreCombat() - Facing closest enemy (we should have a target now)");
                    player.FaceClosestEnemy();

                    // Let's check if we actually got it as our target
                    if (player.HasTarget())
                    {
                        Console.WriteLine("OnPreCombat() - Affirmative. We have a target");
                        /// Ok, we have the target, it's time to start attacking,
                        /// but first we rebuff and drink up just in case
                    }
                    else
                    {
                        // Let's try moving closer
                        Console.WriteLine("OnPreCombat() - Can't target. This should not happen :-P");
                    }
                }
            }
        }

        protected virtual void OnInCombat()
        {
            if (player.IsBeingAttacked())
            {
                /// We are being attacked by a Mob. That means that we should fight back
                /// by finding the mob first of all
                if (player.SelectWhoIsAttackingUs())
                {
                    /// We found who is attacking us and we fight back
                    if (Math.Abs(player.FacingDegrees() - player.AngleToTargetDegrees()) > 20.0f)
                    {
                        player.FaceTarget();
                    }
                    if (player.DistanceFromTarget() > 3.0f)
                    {
                        // we have to get closer (melee only though, we should also check if we're 
                        // using a melee or spell ability)
                    }
                    //paladin.Fight();
                    //player.PlayAction("combat"); // this should call the routine to fight back based on the bindings
                }
            }
        }

        /// <summary>
        /// This happens when a combat has just ended.
        /// </summary>
        protected virtual void OnPostCombat()
        {
            Console.WriteLine("OnPostCombat()");
            // We reset all actions listed as toggle
            foreach (var pair in Actions)
            {
                if (pair.Value.Toggle)
                {
                    pair.Value.Active = false;
                }
            }
            player.AddLastTargetToLootList();
            player.LootClosestLootableMob();

        }

        protected virtual void OnRest()
        {
            Console.WriteLine("OnRest()");
            
            if (NeedMana())
            {
                Drink();
            }
            if (NeedHealth())
            {
                if (CanSelfHeal())
                {
                    // We cast a healing spell on ourselves before eating
                    SelfHeal();
                }
                Eat();
            }
        }

        /// <summary>
        /// This should be impelemented by the derived classes based upon the player class.
        /// </summary>
        protected virtual void Fight()
        {
        }


        #region Rest stuff 

        protected bool NeedMana()
        {
            if (player.MpPct() < MinMPPct)
            {
                return true;
            }
            return false;
        }

        protected bool NeedHealth()
        {
            if (player.HpPct() < MinHPPct)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// This should be implemented by the script of the corresponing player class and return whether
        /// the toon can cast healing spells or not
        /// </summary>
        /// <returns>true if the toon can cast healing spells</returns>
        protected virtual bool IsHealer()
        {
            return false;
        }

        protected bool CanSelfHeal()
        {
            /// We should also have a list of self healing spells with a priority on them
            /// and go through that list and see if we have the mana and if the spell is
            /// not on cooldown
            if (IsHealer())
            {
                return true;
            }
            return false;
        }

        protected virtual void SelfHeal()
        {
            
        }

        protected void Drink()
        {
            /// we should go through our list of drinks and use one of them
        }

        protected void Eat()
        {
            /// we should go through our list of food and use one of them
        }

        #endregion

        #region Consumables

        protected bool HasHealthPotion()
        {
            return false;
        }

        protected bool HasManaPotion()
        {
            return false;
        }

        
        #endregion
    }
}