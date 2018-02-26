// From Equinox: https://gist.github.com/Equinox-/430ce7ac39d1cc0b50941e99960bcadd


using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

public class GridTurrets : IDisposable
{
    private readonly IMyCubeGrid _grid;
    private readonly Dictionary<MyWeaponDefinition, MyObjectBuilder_PhysicalObject[]> _ammoTypes;
    private readonly List<IMyGunBaseUser> _turretsForUpdate;
    private readonly List<IMyGunBaseUser> _turretsSleeping;

    public GridTurrets(IMyCubeGrid grid)
    {
        _ammoTypes = new Dictionary<MyWeaponDefinition, MyObjectBuilder_PhysicalObject[]>();
        _turretsForUpdate = new List<IMyGunBaseUser>();
        _turretsSleeping = new List<IMyGunBaseUser>();
        _grid = grid;
        _grid.OnBlockAdded += OnBlockAdded;
        _grid.OnBlockRemoved += OnBlockRemoved;
    }

    private void OnBlockAdded(IMySlimBlock obj)
    {
        var gun = obj?.FatBlock as IMyGunBaseUser;
        if (gun == null)
            return;
        Register(gun);
    }

    private void OnBlockRemoved(IMySlimBlock obj)
    {
        var gun = obj?.FatBlock as IMyGunBaseUser;
        if (gun == null)
            return;
        Unregister(gun);
    }

    private void Register(IMyGunBaseUser gun)
    {
        var block = (IMyCubeBlock)gun;
        var weapon = WeaponShortcuts.GetWeaponDefinition(block);
        if (weapon == null || !weapon.HasAmmoMagazines())
            return;

        block.IsWorkingChanged += Block_IsWorkingChanged;
        Block_IsWorkingChanged(block);
    }

    private void Unregister(IMyGunBaseUser gun, bool remove = true)
    {
        var block = (IMyCubeBlock)gun;
        block.IsWorkingChanged -= Block_IsWorkingChanged;
        if (remove)
        {
            _turretsForUpdate.Remove(gun);
            _turretsSleeping.Remove(gun);
        }
    }

    private void Block_IsWorkingChanged(IMyCubeBlock obj)
    {
        var gun = obj as IMyGunBaseUser;
        _turretsForUpdate.Remove(gun);
        _turretsSleeping.Remove(gun);
        if (obj.IsWorking)
            _turretsForUpdate.Add(gun);
        else
            _turretsSleeping.Add(gun);
    }

    private static readonly MyFixedPoint _addAmount = 50;
    private static readonly MyFixedPoint _thresholdAmount = 25;
    private int _updateId;
    public void Update(int spread = 1)
    {
        for (var i = _updateId; i < _turretsForUpdate.Count; i += spread)
        {
            var target = _turretsForUpdate[i];
            var inv = target.AmmoInventory;
            if (inv == null)
                continue;
            var entity = (IMyEntity)target;
            var weapon = WeaponShortcuts.GetWeaponDefinition(entity);
            if (weapon == null || !weapon.HasAmmoMagazines())
                continue;

            MyObjectBuilder_PhysicalObject[] ammoMags;
            if (!_ammoTypes.TryGetValue(weapon, out ammoMags))
            {
                ammoMags = new MyObjectBuilder_PhysicalObject[weapon.AmmoMagazinesId.Length];
                for (var j = 0; j < ammoMags.Length; j++)
                    ammoMags[j] = new MyObjectBuilder_AmmoMagazine() { SubtypeName = weapon.AmmoMagazinesId[j].SubtypeName };
                _ammoTypes[weapon] = ammoMags;
            }
            foreach (var mag in ammoMags)
            {
                if (inv.GetItemAmount(mag.GetObjectId(), mag.Flags) > _thresholdAmount)
                    continue;
                inv.AddItems(_addAmount, mag);
            }
        }
        _updateId = (_updateId + 1) % spread;
    }

    public void Dispose()
    {
        _grid.OnBlockAdded -= OnBlockAdded;
        _grid.OnBlockRemoved -= OnBlockRemoved;
        foreach (var x in _turretsForUpdate)
            Unregister(x, false);
        _turretsForUpdate.Clear();
        foreach (var x in _turretsSleeping)
            Unregister(x, false);
        _turretsSleeping.Clear();
        _ammoTypes.Clear();
    }
}

public static class WeaponShortcuts
{
    public static MyWeaponDefinition GetWeaponDefinition(IMyEntity ent)
    {
        try
        {
            var block = ent as IMyCubeBlock;
            if (block != null && ent is IMyGunBaseUser)
            {
                var def = MyDefinitionManager.Static.GetCubeBlockDefinition(block.BlockDefinition);
                var wep = def as MyWeaponBlockDefinition;
                if (wep != null)
                    return MyDefinitionManager.Static.GetWeaponDefinition(wep.WeaponDefinitionId);
                return MyDefinitionManager.Static.GetWeaponDefinition(
                    GetBackwardCompatibleDefinitionId(def.Id.TypeId));
            }

            var gun = ent as IMyHandheldGunObject<MyToolBase>;
            if (gun != null)
            {
                var def = gun.PhysicalItemDefinition;
                var pdef = def as MyWeaponItemDefinition;
                if (pdef != null)
                    return MyDefinitionManager.Static.GetWeaponDefinition(pdef.WeaponDefinitionId);
            }
        }
        catch
        {
            // ignored
        }
        return null;
    }

    private static MyDefinitionId GetBackwardCompatibleDefinitionId(MyObjectBuilderType typeId)
    {
        if (typeId == typeof(MyObjectBuilder_LargeGatlingTurret))
        {
            return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "LargeGatlingTurret");
        }
        if (typeId == typeof(MyObjectBuilder_LargeMissileTurret))
        {
            return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "LargeMissileTurret");
        }
        if (typeId == typeof(MyObjectBuilder_InteriorTurret))
        {
            return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "LargeInteriorTurret");
        }
        if (typeId == typeof(MyObjectBuilder_SmallMissileLauncher) || typeId == typeof(MyObjectBuilder_SmallMissileLauncherReload))
        {
            return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "SmallMissileLauncher");
        }
        if (typeId == typeof(MyObjectBuilder_SmallGatlingGun))
        {
            return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "GatlingGun");
        }
        return default(MyDefinitionId);
    }
}