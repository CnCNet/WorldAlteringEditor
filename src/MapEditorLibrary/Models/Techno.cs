using Microsoft.Xna.Framework;

namespace MapEditorLibrary.Models;

public abstract class Techno<T> : TechnoBase where T : TechnoType
{
    public Techno(T objectType)
    {
        ObjectType = objectType;
    }

    public override GameObjectType GetObjectType() => ObjectType;

    public override double GetWeaponRange() => ObjectType.GetWeaponRange();

    public override double GetGuardRange()
    {
        return ObjectType.GuardRange > 0.0 ? ObjectType.GuardRange : GetWeaponRange();
    }

    public override double GetGapGeneratorRange()
    {
        return ObjectType.GapGenerator ? ObjectType.GapRadiusInCells : 0.0;
    }

    public override double GetCloakGeneratorRange() => 0.0;

    public override double GetSensorArrayRange() => 0.0;

    public override Color GetRadialColor() => Owner?.XNAColor ?? Color.White;

    public override bool IsInvisibleInGame() => ObjectType.InvisibleInGame;

    public T ObjectType { get; }
}

public abstract class TechnoBase : GameObject
{
    public TechnoBase()
    {
        HP = Constants.ObjectHealthMax;
    }

    public virtual House Owner { get; set; }

    /// <summary>
    /// Unique object ID for this instance.
    /// Assigned when the object is placed on map.
    /// When zero or negative, this object should not be considered as placed on the map.
    /// </summary>
    public int ObjectId { get; set; }

    public int HP { get; set; }
    public virtual byte Facing { get; set; }
    public Tag AttachedTag { get; set; }

    public override AbstractObject Clone()
    {
        var clone = (TechnoBase)base.Clone();
        clone.ObjectId = 0;
        return clone;
    }

    public abstract double GetWeaponRange();
    public abstract double GetGuardRange();
    public abstract double GetGapGeneratorRange();
    public abstract double GetCloakGeneratorRange();
    public abstract double GetSensorArrayRange();
    public abstract Color GetRadialColor();

    public override Color GetRemapColor() => Remapable() ? Owner.XNAColor : Color.White;
}
