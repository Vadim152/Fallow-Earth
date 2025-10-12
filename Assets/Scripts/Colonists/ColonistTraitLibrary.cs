using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ColonistTraitLibrary
{
    private static readonly Dictionary<string, ColonistTrait> traits = new Dictionary<string, ColonistTrait>();

    static ColonistTraitLibrary()
    {
        var stoic = new ColonistTrait("Stoic", "Stays calm under pressure", 0.05f);
        stoic.AddNeedEffect(NeedType.Stress, 0.8f, -0.1f);
        traits[stoic.Name] = stoic;

        var gourmand = new ColonistTrait("Gourmand", "Gets hangry quickly", -0.05f);
        gourmand.AddNeedEffect(NeedType.Hunger, 1.2f, 0.1f);
        traits[gourmand.Name] = gourmand;

        var socialite = new ColonistTrait("Socialite", "Thrives on company", 0.1f);
        socialite.AddNeedEffect(NeedType.Social, 1.3f, 0.1f);
        traits[socialite.Name] = socialite;
    }

    public static ColonistTrait CreateTrait(string name)
    {
        if (name == null)
            return null;
        if (!traits.TryGetValue(name, out ColonistTrait template))
            return null;
        var clone = new ColonistTrait(template.Name, template.Description, template.MoodModifier);
        foreach (var effect in template.NeedEffects)
            clone.AddNeedEffect(effect.type, effect.multiplier, effect.expectationOffset);
        return clone;
    }

    public static ColonistTrait GetRandomTrait()
    {
        if (traits.Count == 0)
            return null;
        var values = traits.Values.ToList();
        var chosen = values[Random.Range(0, values.Count)];
        return CreateTrait(chosen.Name);
    }

    public static IEnumerable<string> AllTraitNames => traits.Keys;
}
