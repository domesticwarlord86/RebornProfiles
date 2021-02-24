using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;

namespace Kupo.Helpers
{
   public static class KupoExtensions
    {
        public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dic,Func<TValue, bool> predicate)
        {
            var keys = dic.Keys.Where(k => predicate(dic[k])).ToList();
            foreach (var key in keys)
            {
                dic.Remove(key);
            }
        }
        public static void RemoveAllKeys<TKey, TValue>(this Dictionary<TKey, TValue> dic, Func<TKey, bool> predicate)
        {
            var keys = dic.Keys.Where(predicate).ToList();
            foreach (var key in keys)
            {
                dic.Remove(key);
            }
        }


        public static void UpdateDoubleCastDict(string spellName, GameObject unit)
        {
            if (unit == null)
                return;

            SpellData spellData;


            ActionManager.CurrentActions.TryGetValue(spellName, out spellData);


            if (spellData == null)
                return;


            DateTime expir = DateTime.UtcNow + spellData.AdjustedCastTime + TimeSpan.FromSeconds(3);
            string key = DoubleCastKey(unit.ObjectId, spellName);
            if (DoubleCastPreventionDict.ContainsKey(key))
                DoubleCastPreventionDict[key] = expir;

            DoubleCastPreventionDict.Add(key, expir);
        }

        public static string DoubleCastKey(uint guid, string spellName)
        {
            return guid.ToString("X") + "-" + spellName;
        }

        public static string DoubleCastKey(GameObject unit, string spell)
        {
            return DoubleCastKey(unit.ObjectId, spell);
        }

        public static bool Contains(this Dictionary<string, DateTime> dict, GameObject unit, string spellName)
        {
            return dict.ContainsKey(DoubleCastKey(unit, spellName));
        }

        public static bool ContainsAny(this Dictionary<string, DateTime> dict, GameObject unit,
            params string[] spellNames)
        {
            return spellNames.Any(s => dict.ContainsKey(DoubleCastKey(unit, s)));
        }

        public static bool ContainsAll(this Dictionary<string, DateTime> dict, GameObject unit,
            params string[] spellNames)
        {
            return spellNames.All(s => dict.ContainsKey(DoubleCastKey(unit, s)));
        }

        public static readonly Dictionary<string, DateTime> DoubleCastPreventionDict = new Dictionary<string, DateTime>();



    

    }
}