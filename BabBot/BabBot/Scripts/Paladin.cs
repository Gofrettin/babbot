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
using System;
using BabBot.Bot;
using BabBot.Scripting;

namespace BabBot.Scripts
{
    public class Toon : Script, IScript
    {
        #region IScript Members

        void IScript.Init()
        {
            Console.WriteLine("Paladin->Init()");
            // TODO: find a way to override this function so that we don't have to clone those lines
            Bindings = new BindingList();
            Actions = new PlayerActionList();
            HealingSpells = new SpellList();

            // TODO: get that stuff out of the way and load bindings from the appropriate XML file
            var b = new Binding("melee", 1, "1");
            Bindings.Add(b.Name, b);
            b = new Binding("test", 1, "2");
            Bindings.Add(b.Name, b);

            var a = new PlayerAction("attack", Bindings["melee"], 0.0f, 0.0f, false, true);
            Actions.Add(a.Name, a);
            a = new PlayerAction("fakeattack", Bindings["test"], 0.0f, 2.0f, true, true);
            Actions.Add(a.Name, a);

            // Populate the list of our healing spells
            var s = new Spell("Holy Light", 1);
            HealingSpells.Add(s);
            // TODO: We should sort the list by weight now 
        }

        #endregion

        #region Paladin specific settings

        protected string multiAura = "Retribution Aura";
        protected int HpHammer = 40;
        protected int HpPctEmergency = 25;
        protected int exorcismHp = 100;
        protected bool useExorcism = true;
        protected bool combatDebuff = false;

        #endregion

        protected override bool IsHealer()
        {
            return true;
        }

        protected override void SelfHeal()
        {
            Console.WriteLine("SelfHeal()");
            foreach (var spell in HealingSpells)
            {
                if (!spell.IsOnCooldown() && (spell.Cost < player.Mp()))
                {
                    Console.WriteLine("SelfHeal() - Casting " + spell.Name);
                    player.CastSpellByName(spell.Name, true);
                }
            }
        }

        // TODO: add an interface for all common functions like this
        protected override void Fight()
        {
            // TODO: Implement actual fight logic (only by PlayAction)




            //Console.WriteLine("attack");
            //player.PlayAction(Actions["attack"], true);
            //Console.WriteLine("fakeattack");
            //player.PlayAction(Actions["fakeattack"]);
        }

        // TODO: with some refactoring we could use this as a generic routine
        protected override void OnInCombat()
        {
            Console.WriteLine("Paladin->OnInCombat()");

            if (!player.HasTarget() || player.IsTargetDead())
            {
                return;
            }

            // TODO: Before uncommenting this part we need to work out a FindBestTarget routine
            /*
            if (player.IsTargetTapped() && !player.IsTargetTappedByMe())
            {
                player.SpellStopCasting();
                player.ClearTarget();
                player.FindBestTarget();
                player.AttackTarget();
            }
            */

            player.FaceTarget();

            if (player.DistanceFromTarget() > MaxMeleeDistance)
            {
                Console.WriteLine("Paladin->OnInCombat() - Moving towards target");
                player.FaceTarget();
                player.MoveToTarget(MinMeleeDistance);
                return;
            }

            if (player.IsMoving() && player.DistanceFromTarget() < MaxMeleeDistance && player.DistanceFromTarget() > MinMeleeDistance)
            {
                player.Stop();
            }

            if (player.DistanceFromTarget() < MinMeleeDistance)
            {
                player.MoveBackward(300);
            }

            if (!player.IsAttacking())
            {
                player.AttackTarget();
            }

            if (player.HpPct() <= HpPctEmergency)
            {
                Emergency();
            }

            if (player.HpPct() <= HpPctPotion && HasHealthPotion())
            {
                player.SpellStopCasting();
                // TODO: implement a function to select the best health potion and drink it
                //player.TakePotion("HP");
            }

            if (player.MpPct() <= MpPctPotion && HasManaPotion())
            {
                player.SpellStopCasting();
                // TODO: implement a function to select the best mana potion and drink it
                //player.TakePotion("MP");
            }

            if (player.HpPct() <= HpHammer && player.MpPct() > 5 && player.HpPct() >= HpPctEmergency && player.CanCast("Hammer of Justice"))
            {
                player.CastSpellByName("Hammer of Justice");

                if (player.TargetHpPct() > 10)
                {
                    HealSystem();
                    Console.WriteLine("Hammer of Justice healing");
                    return;
                }
                return;
            }

            // TODO: implement IsBeingAttackedByManyMobs by going through all the mobs who have aggro on us and are in range
            /*
            if (player.IsBeingAttackedByManyMobs())
            {
                if (player.CanCast(multiAura))
                {
                    player.CastSpellByName(multiAura);
                }

                if (player.CanCast("Consecration"))
                {
                    player.CastSpellByName("Consecration");
                }
            }
            */

            // TODO: implement TargetIsCasting()
            /*
            if (player.TargetIsCasting())
            {
                CastInterruption();
            }
            */

            if (player.TargetHpPct() <= 20)
            {
                FinalFight();
            }
            else
            {
                NormalFight();
            }


            // TODO: implement TargetCreatureType()
            /*
            if (useExorcism)
            {
                if (player.MpPct() > 30 && player.TargetHpPct() > exorcismHp && player.CanCast("Exorcism") && player.TargetCreatureType() == "Undead" || player.TargetCreatureType() == "Demon")
                {
                    player.CastSpellByName("Exorcism");
                }
            }
            */

            if (combatDebuff)
            {
                DebuffAll();
            }

            // TODO: implement reading of the player race
            /*
            if (player.Race() == "Blood Elf")
            {
                if (player.Buff("Mana Tap").Application > 0 && player.MpPct() <= 20)
                {
                    player.SpellStopCasting();
                    player.CastSpellByName("Arcane Torrent");
                }
                if (player.TargetMpPct() > 0 && player.IsActionUsable("Mana Tap") && player.Buff("Mana Tap").Application < 3) 
                {
                    player.CastSpellByName("Mana Tap");
                }
            }
             */
        }


        protected void Emergency()
        {
        }

        protected void HealSystem()
        {
        }

        protected void CastInterruption()
        { 
        }

        protected void FinalFight()
        {
        }

        protected void NormalFight()
        {
        }

        protected void DebuffAll()
        {
        }

    }
}