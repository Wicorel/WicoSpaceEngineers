﻿//V 2.3 Mar 14 2018
// V2.4 May 15 2018.  Min/Max values
// V 2.5 Jan 13-24, 2019 SE V 1.189 

// V2.6 Sep 17, 2021 

// V2.7 Feb 23, 2022 SE 1.200 

// SE 2.202.002 removed IMyTurretControlBlock ?

//MyDefinitionId electricDefId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Electricity");
//MyDefinitionId oxygenDefId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Oxygen");
//MyDefinitionId hydrogenDefId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");

void Main(string sArgument)
{
    
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    IMyTextPanel screen = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD Test");
    if (screen == null)
    {
        List<IMyTerminalBlock> gtsTestBlocks = new List<IMyTerminalBlock>();
        GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(gtsTestBlocks);
        if (gtsTestBlocks.Count > 0)
            screen = (IMyTextPanel)gtsTestBlocks[0];
    }
    if (screen != null)
    {
        StringBuilder values = new StringBuilder();
        StringBuilder test = new StringBuilder();
        if (sArgument == "")
        {
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);
        }
        else
        {
            Echo("Searching for blocks named:'" + sArgument + "'");
            GridTerminalSystem.SearchBlocksOfName(sArgument, blocks);
        }

        values.Append("\n" + Me.CubeGrid.CustomName + ":" + Me.CubeGrid.EntityId.ToString()+"\n");
        values.Append(Me.GetPosition().ToString()+"\n");

        Echo("Found :" + blocks.Count.ToString());
        if (blocks.Count > 0)
        {
            for (int iBlock = 0; iBlock < blocks.Count; iBlock++)
            {
                DisplayBlockInfo(ref values, blocks[iBlock]);
            }
        }
        else
            values.Append("No Blocks Found");

        //        screen.SetValue("FontSize", 0.8f);
        //      screen.ShowTextureOnScreen();
        //        screen.WritePublicText(values.ToString());
        //        screen.ShowPublicTextOnScreen();
        screen.WriteText(values.ToString());
    }
    else Echo("No Screen Found");
}

void DisplayBlockInfo(ref StringBuilder values, IMyTerminalBlock unit)
{
//        StringBuilder values = new StringBuilder();
    if (unit.CustomName.ToLower().Contains("skip") || unit.CustomData.ToLower().Contains("skip"))
    {
        Echo("Skipping: " + unit.CustomName);
        return;
    }

    List<ITerminalAction> resultList = new List<ITerminalAction>();

    Echo(unit.CustomName);
    unit.GetActions(resultList);
    
    values.Append(unit.CustomName + "\n");
    values.Append(unit.ToString() + "\n");
    values.Append(unit.EntityId.ToString() + "\n");

    values.Append("TyepID=" + unit.BlockDefinition.TypeIdString + "\n");
    values.Append("SubtyepID=" + unit.BlockDefinition.SubtypeId + "\n");
    values.Append("Mass=" + unit.Mass.ToString() + "\n");
    values.Append("IsBeingHacked =" + unit.IsBeingHacked.ToString() + "\n");
    values.Append("IsWorking =" + unit.IsWorking.ToString() + "\n");
    values.Append("IsFunctional =" + unit.IsFunctional.ToString() + "\n");
    values.Append("DisassembleRatio =" + unit.DisassembleRatio.ToString() + "\n");
    values.Append("DisplayNameText =" + unit.DisplayNameText.ToString() + "\n");
    values.Append("\nActions:\n");

    Echo("#Results="+resultList.Count.ToString());
    for (int i = 0; i < resultList.Count; i++)
    {
        StringBuilder temp = new StringBuilder();
//        Echo(resultList[i].Id.ToString());
  //      Echo(resultList[i].Name.ToString());
        values.Append(resultList[i].Id + ":" + resultList[i].Name + "(");
  //      Echo(resultList[i].Id + ":" + resultList[i].Name + "(");
        if (resultList[i].Id.Length == 0)
            resultList[i].WriteValue(unit, temp);
        else
        {
//            Echo(resultList[i].Id.ToString());
            unit.GetActionWithName(resultList[i].Id.ToString()).WriteValue(unit, temp);
        }
//        Echo(temp.ToString());
        values.Append(temp.ToString());
        values.Append(")\n");
    }
//    Echo("AA1");
    List<ITerminalProperty> propList = new List<ITerminalProperty>();
    unit.GetProperties(propList);
//    Echo("AA2");
    Echo("#Properties=" + propList.Count.ToString());
    values.Append("\nProperties:\n");
//    Echo("AA3");
    for (int i = 0; i < propList.Count; i++)
    {
//        Echo(i + ":" + propList[i].TypeName);
//        Echo("AA4:"+i.ToString());

        values.Append(propList[i].Id + ":" + propList[i].TypeName);
        Echo(propList[i].Id + ":" + propList[i].TypeName);
        if (propList[i].TypeName == "Boolean")
        {
            bool b = unit.GetValueBool(propList[i].Id);
            values.Append(" (" + b + ")");
        }
        else if (propList[i].TypeName == "Single")
        {
            float f = unit.GetValueFloat(propList[i].Id);
            float fMax=unit.GetMaximum<float>(propList[i].Id);
            float fMin = unit.GetMinimum<float>(propList[i].Id);
            values.Append(" (" + f + ") Valid Range: " + fMin + "->" + fMax);
        }
        else if (propList[i].TypeName == "StringBuilder")
        {
            string s = unit.GetValue<StringBuilder>(propList[i].Id).ToString();
            values.Append(" (" + s + ")");
        }
        else if (propList[i].TypeName == "Int64")
        {
            long l = unit.GetValue<long>(propList[i].Id);
            long Max = unit.GetMaximum<long>(propList[i].Id);
            long Min = unit.GetMinimum<long>(propList[i].Id);
            values.Append(" (" + l + ") Valid Range: " + Min + "->" + Max);
//            values.Append(" (" + l + ")");
        }
        else if (propList[i].TypeName == "HashSet`1")
        { // from Cheetah's radar mod. http://steamcommunity.com/sharedfiles/filedetails/?id=907384096
            HashSet<MyDetectedEntityInfo> hsInfo = unit.GetValue<HashSet<MyDetectedEntityInfo>>(propList[i].Id);
            if (hsInfo == null)
            {
                // NavBall
                Echo("NOT deiinfo");
            }
            else
            {
                values.Append(" (" + hsInfo.Count + " Entries)");
            //						if (hsInfo.Count > 0) values.AppendLine();
            foreach (var myDei in hsInfo)//int i2=0;i2<hsInfo.Count; i2++)
            {
                if (myDei.IsEmpty())
                {
                    values.AppendLine(); values.Append(" EMPTY!");
                }
                else
                {
                    values.AppendLine(); values.Append(" Name: " + myDei.Name);
                    values.AppendLine(); values.Append(" Type: " + myDei.Type);
                    values.AppendLine(); values.Append(" RelationShip: " + myDei.Relationship);
                    //							values.AppendLine();		values.Append(" Size: " + myDei.BoundingBox.Size);
                    //							values.AppendLine();		values.Append(" Velocity: " + myDei.Velocity);
                    //							values.AppendLine();		values.Append(" Orientation: " + myDei.Orientation);
                    values.AppendLine(); values.Append(" ---");
                }
            }
            }
        }

        values.AppendLine();
    }
    Echo("End of Properties");

    values.Append("\nDetailedInfo:\n" + unit.DetailedInfo);
    values.Append("\n----------\n");

    values.Append("\nCustomInfo=" + unit.CustomInfo);
    values.Append("\nCustomName=" + unit.CustomName);
    values.Append("\nCustomData=" + unit.CustomData);
    values.Append("\nCustomNameWithFaction=" + unit.CustomNameWithFaction);
    values.Append("\nShowOnHUD=" + unit.ShowOnHUD);
    values.Append("\n");

    if (unit is IMyFunctionalBlock)
    {
        IMyFunctionalBlock ipp = unit as IMyFunctionalBlock;
        values.Append("\nIMyFunctionalBlock");
        values.Append("\n Enabled=" + ipp.Enabled.ToString());
        values.Append("\n");
    }
    //readded in 1.189 with different members
    if (unit is IMyPowerProducer)
    {
        IMyPowerProducer ipp = unit as IMyPowerProducer;
        
        values.Append("\nIMyPowerProducer");
        values.Append("\n CurrentOutput=" + ipp.CurrentOutput.ToString());
//        values.Append("\n DefinedOutput=" + ipp.DefinedPowerOutput.ToString());
        values.Append("\n MaxOutput=" + ipp.MaxOutput.ToString());
//        values.Append("\n ProductionEnabled=" + ipp.ProductionEnabled.ToString());
        values.Append("\n");
    }
    if (unit is IMyTextPanel)
    {
        values.Append("\nIMyTextPanel");
        values.Append("\n");
    }

    if (unit is IMyTextSurfaceProvider)
    {
            IMyTextSurfaceProvider ipp = unit as IMyTextSurfaceProvider;
            values.Append("\nIMyTextSurfaceProvider");
            values.Append("\n SurfaceCount=" + ipp.SurfaceCount.ToString());
            for(int i=0;i<ipp.SurfaceCount;i++)
            {
                IMyTextSurface ts = ipp.GetSurface(i);
                values.Append("\n Surface "+i.ToString());
                values.Append("\n  DisplayName=" + ts.DisplayName.ToString());
                values.Append("\n  Name=" + ts.Name.ToString());
                values.Append("\n  SurfaceSize=" + ts.SurfaceSize.ToString());
                values.Append("\n  TextureSize=" + ts.TextureSize.ToString());

            }
            values.Append("\n");

        }
        if (unit is IMyBatteryBlock)
    {
        IMyBatteryBlock ipp = unit as IMyBatteryBlock;
        values.Append("\nIMyBatteryBlock");
        values.Append("\n CurrentStoredPower=" + ipp.CurrentStoredPower.ToString());
        values.Append("\n HasCapacityRemaining=" + ipp.HasCapacityRemaining.ToString());
        values.Append("\n MaxStoredPower=" + ipp.MaxStoredPower.ToString());
        values.Append("\n MaxOutput=" + ipp.MaxOutput.ToString());
        values.Append("\n ChargeMode=" + ipp.ChargeMode.ToString());
        /*
        values.Append("\n IsCharging=" + ipp.IsCharging.ToString());
        values.Append("\n OnlyDischarge=" + ipp.OnlyDischarge.ToString());
        values.Append("\n OnlyRecharge=" + ipp.OnlyRecharge.ToString());
        values.Append("\n SemiautoEnabled=" + ipp.SemiautoEnabled.ToString());
        */

        values.Append("\n");
    }
    if (unit is IMyGasTank)
    {
        IMyGasTank imgt = unit as IMyGasTank;
        values.Append("\nIMyGasTank");
        values.Append("\n AutoRefillBottles=" + imgt.AutoRefillBottles.ToString());
        values.Append("\n Capacity=" + imgt.Capacity.ToString());
        values.Append("\n FilledRatio=" + imgt.FilledRatio.ToString());
        values.Append("\n Stockpile=" + imgt.Stockpile.ToString());
        values.Append("\n");
    }
    if (unit is IMyThrust)
    {
        IMyThrust mythruster = unit as IMyThrust;
        values.Append("\nIMyThrust");
        values.Append("\n ThrustOverride=" + mythruster.ThrustOverride.ToString() + " newtons");
        values.Append("\n CurrentThrust=" + mythruster.CurrentThrust.ToString() + " newtons");
        values.Append("\n GridThrustDirection=" + mythruster.GridThrustDirection.ToString());
        values.Append("\n MaxEffectiveThrust=" + mythruster.MaxEffectiveThrust.ToString() + " newtons");
        values.Append("\n MaxThrust=" + mythruster.MaxThrust.ToString() + " newtons");
        values.Append("\n ThrustOverridePercentage=" + mythruster.ThrustOverridePercentage.ToString() + " 0->1 %");
        values.Append("\n");
    }
    if (unit is IMyReactor)
    {
        IMyReactor ipp = unit as IMyReactor;
        values.Append("\nIMyReactor");
        values.Append("\n CurrentOutput=" + ipp.CurrentOutput.ToString());
        values.Append("\n MaxOutput=" + ipp.MaxOutput.ToString());
        values.Append("\n");
    }
    if (unit is IMySolarPanel)
    {
        IMySolarPanel ipp = unit as IMySolarPanel;
        values.Append("\nIMySolarPanel");
        values.Append("\n CurrentOutput=" + ipp.CurrentOutput.ToString());
        values.Append("\n MaxOutput=" + ipp.MaxOutput.ToString());
        values.Append("\n");
    }

    if (unit is IMyParachute)
    {
        IMyParachute ipp = unit as IMyParachute;
        values.Append("\nIMyParachute");
        values.Append("\n Atmosphere=" + ipp.Atmosphere.ToString());
        values.Append("\n GetNaturalGravity()=" + ipp.GetNaturalGravity().ToString());
        values.Append("\n OpenRatio=" + ipp.OpenRatio.ToString());
        values.Append("\n Status=" + ipp.Status.ToString());
        values.Append("\n");
    }
    if (unit is IMyDoor)
    {
        IMyDoor imd = unit as IMyDoor;
        values.Append("\nIMyDoor ");
        values.Append("\n Status=" + imd.Status.ToString());
        values.Append("\n OpenRatio=" + imd.OpenRatio.ToString());
        values.Append("\n");
    }
    if (unit is IMyAdvancedDoor)
    {
        IMyAdvancedDoor imd = unit as IMyAdvancedDoor;
        values.Append("\nIMyAdvancedDoor ");
        values.Append("\n");
    }
    
    if (unit is IMyAirtightHangarDoor)
    {
        IMyAirtightHangarDoor imd = unit as IMyAirtightHangarDoor;
        values.Append("\nIMyAirtightHangarDoor ");
        values.Append("\n");
    }

    if (unit is IMyAirtightSlideDoor)
    {
        IMyAirtightSlideDoor imd = unit as IMyAirtightSlideDoor;
        values.Append("\nIMyAirtightSlideDoor ");
        values.Append("\n");
    }
    if (unit is IMyMotorSuspension)
    {
        IMyMotorSuspension ipp = unit as IMyMotorSuspension;
        values.Append("\nIMyMotorSuspension");
        values.Append("\n Brake=" + ipp.Brake.ToString());
//        values.Append("\n Damping=" + ipp.Damping.ToString()); Removed 1.186
        values.Append("\n Friction=" + ipp.Friction.ToString());
        values.Append("\n Height=" + ipp.Height.ToString());
        values.Append("\n InvertSteer=" + ipp.InvertSteer.ToString());
        values.Append("\n MaxSteerAngle=" + ipp.MaxSteerAngle.ToString());
        values.Append("\n Power=" + ipp.Power.ToString());
        float maxPower = ipp.GetMaximum<float>("Power");
        values.Append("\n  MaxPower=" + maxPower.ToString());

        values.Append("\n Propulsion=" + ipp.Propulsion.ToString());
        values.Append("\n SteerAngle=" + ipp.SteerAngle.ToString());
        values.Append("\n Steering=" + ipp.Steering.ToString());
        //        values.Append("\n SteerReturnSpeed=" + ipp.SteerReturnSpeed.ToString()); Removed 1.186
        //        values.Append("\n SteerSpeed=" + ipp.SteerSpeed.ToString()); Removed 1.186
        values.Append("\n Strength=" + ipp.Strength.ToString());
        //        values.Append("\n SuspensionTravel=" + ipp.SuspensionTravel.ToString()); Removed 1.186
        values.Append("\n");
    }
    if (unit is IMyMotorStator)
    {
        IMyMotorStator ipp = unit as IMyMotorStator;
        values.Append("\nIMyMotorStator");
        values.Append("\n Angle=" + ipp.Angle.ToString());
        values.Append("\n BrakingTorque=" + ipp.BrakingTorque.ToString());
        values.Append("\n Displacement=" + ipp.Displacement.ToString());
        values.Append("\n IsAttached=" + ipp.IsAttached.ToString());
//        values.Append("\n LowerLimit=" + ipp.LowerLimit.ToString());
        values.Append("\n Torque=" + ipp.Torque.ToString());
 //       values.Append("\n UpperLimit=" + ipp.UpperLimit.ToString());
//        values.Append("\n Velocity=" + ipp.TargetVelocity.ToString());

        float minVelocity = ipp.GetMinimum<float>("Velocity");
        values.Append("\n  minVelocity=" + minVelocity.ToString());

        float maxVelocity = ipp.GetMaximum<float>("Velocity");
        values.Append("\n  maxVelocity=" + maxVelocity.ToString());


        values.Append("\n");
    }
    if (unit is IMyShipController)
    {
        IMyShipController ipp = unit as IMyShipController;
        values.Append("\nIMyShipController");
        values.Append("\n ControlThrusters =" + ipp.ControlThrusters.ToString());
        values.Append("\n ControlWheels =" + ipp.ControlWheels.ToString());
        values.Append("\n DampenersOverride =" + ipp.DampenersOverride.ToString());
        values.Append("\n HandBrake =" + ipp.HandBrake.ToString());
        values.Append("\n IsUnderControl =" + ipp.IsUnderControl.ToString());
        // lots more things available...
        values.Append("\n");
    }
    if (unit is IMyCockpit)
    {
        values.Append("\nIMyCockpit");
        IMyCockpit io = unit as IMyCockpit;
        MyShipMass myMass = io.CalculateShipMass();
        values.Append("\n OxygenCapacity=" + io.OxygenCapacity.ToString());
        values.Append("\n OxygenFilledRatio=" + io.OxygenFilledRatio.ToString());
        values.Append("\n");
    }
    if (unit is IMyCryoChamber)
    {
        values.Append("\nIMyCryoChamber");
        values.Append("\n");
    }

    if(unit is IMyRemoteControl)
    {
        values.Append("\nIMyRemoteControl");
        IMyRemoteControl rc = unit as IMyRemoteControl;
//        MyWaypointInfo wpi = rc.CurrentWaypoint;
//        values.Append("\n CurrentWaypoint=" + wpi.ToString());
        values.Append("\n FlightMode=" + rc.FlightMode.ToString());
        values.Append("\n IsAutoPilotEnabled=" + rc.IsAutoPilotEnabled.ToString());
        values.Append("\n SpeedLimit=" + rc.SpeedLimit.ToString());

        values.Append("\n");

    }
    if (unit is IMyAssembler)
    {
        IMyAssembler ipp = unit as IMyAssembler;
        values.Append("\nIMyAssembler");
        // OBSOLETE values.Append("\n DisassembleEnabled="+ipp.DisassembleEnabled.ToString()); 
        values.Append("\n Mode=" + ipp.Mode.ToString());
        values.Append("\n CoopMode=" + ipp.CooperativeMode.ToString());
        values.Append("\n CurrentProgress=" + ipp.CurrentProgress.ToString());
        values.Append("\n");
    }
    if (unit is IMyProductionBlock)
    {
        IMyProductionBlock ipp = unit as IMyProductionBlock;
        values.Append("\nIMyProductionBlock");
        values.Append("\n IsProducing=" + ipp.IsProducing.ToString());
        values.Append("\n IsQueueEmpty=" + ipp.IsQueueEmpty.ToString());
        values.Append("\n NextItemId=" + ipp.NextItemId.ToString());
        values.Append("\n UseConveyorSystem=" + ipp.UseConveyorSystem.ToString());
        values.Append("\n");
    }

    if (unit.HasInventory)
    {
        values.Append("\nHasInventory");
        //					values.Append("\n UseConveyorSystem="+unit.UseConveyorSystem.ToString());
        values.Append("\n InventoryCount=" + unit.InventoryCount.ToString());
        for (int i = 0; i < unit.InventoryCount; i++)
        {
            IMyInventory inv = unit.GetInventory(i);
            if (inv != null)
            {
                values.Append("\n\nIMyInventory[" + i + "]");

                values.Append("\n CurrentMass=" + inv.CurrentMass.ToString());
                values.Append("\n CurrentVolume=" + inv.CurrentVolume.ToString());
                values.Append("\n IsFull=" + inv.IsFull.ToString());
                values.Append("\n MaxVolume=" + inv.MaxVolume.ToString());
            }
        }
        values.Append("\n");
    }
    else values.Append("\nNo Inventory\n");

    /*
                    if(unit is IMyInventoryOwner)
                    {
                        values.Append("\nIMyInventoryOwner");

                        IMyInventoryOwner io = unit as IMyInventoryOwner;
                        values.Append("\n UseConveyorSystem="+io.UseConveyorSystem.ToString());
                        values.Append("\n InventoryCount="+io.InventoryCount.ToString());
                        for(int i=0;i<io.InventoryCount; i++)
                        {
                            IMyInventory inv = io.GetInventory(i);
                            if(inv!=null)
                            {
                                values.Append("\n\nIMyInventory[" + i + "]");

                                values.Append("\n CurrentMass="+inv.CurrentMass.ToString());
                                values.Append("\n CurrentVolume="+inv.CurrentVolume.ToString());
                                values.Append("\n IsFull="+inv.IsFull.ToString());
                                values.Append("\n MaxVolume="+inv.MaxVolume.ToString());

                                values.Append("\n Size="+inv.Size.ToString());

                            }
                        }
                        values.Append("\n");
                    }
     */
    if (unit is IMyProjector)
    {
        values.Append("\nIMyProjector");

        IMyProjector io = unit as IMyProjector;
        /* OBSOLETE
                            values.Append("\n ProjectionOffsetX="+io.ProjectionOffsetX.ToString());
                            values.Append("\n ProjectionOffsetY="+io.ProjectionOffsetY.ToString());
                            values.Append("\n ProjectionOffsetZ="+io.ProjectionOffsetZ.ToString());
                            values.Append("\n ProjectionRotX="+io.ProjectionRotX.ToString());
                            values.Append("\n ProjectionRotY="+io.ProjectionRotY.ToString());
                            values.Append("\n ProjectionRotZ="+io.ProjectionRotZ.ToString());
        */
        values.Append("\n ProjectionOffset=" + io.ProjectionOffset.ToString());
        values.Append("\n ProjectionRotation=" + io.ProjectionRotation.ToString());
        values.Append("\n RemainingBlocks=" + io.RemainingBlocks.ToString());
         values.Append("\n");
   }
    if (unit is IMyGyro)
    {
        values.Append("\nIMyGyro");

        IMyGyro io = unit as IMyGyro;
        values.Append("\n GyroOverride=" + io.GyroOverride.ToString());
        values.Append("\n GyroPower=" + io.GyroPower.ToString());
        values.Append("\n Pitch=" + io.Pitch.ToString());
        values.Append("\n Roll=" + io.Roll.ToString());
        values.Append("\n Yaw=" + io.Yaw.ToString());
        values.Append("\n");
    }
    if (unit is IMyPistonBase)
    {
        values.Append("\nIMyPistonBase");

        IMyPistonBase io = unit as IMyPistonBase;
        values.Append("\n CurrentPosition=" + io.CurrentPosition.ToString());
        values.Append("\n MaxLimit=" + io.MaxLimit.ToString());
        values.Append("\n MinLimit=" + io.MinLimit.ToString());
        values.Append("\n Status=" + io.Status.ToString());
        values.Append("\n Velocity=" + io.Velocity.ToString());
        values.Append("\n");
    }
    if (unit is IMyHeatVent)
    {
        IMyHeatVent ihv = unit as IMyHeatVent;
        values.Append("\nIMyHeatVent ");
        values.Append("\n");
    }
    if (unit is IMyLargeGatlingTurret)
    {
        IMyLargeGatlingTurret ihv = unit as IMyLargeGatlingTurret;
        values.Append("\nIMyLargeGatlingTurret ");
        values.Append("\n");
    }
    /* 1.202.002 prohibited
    if (unit is IMyTurretControlBlock)
    {
        IMyTurretControlBlock ihv = unit as IMyTurretControlBlock;
        values.Append("\nIMyTurretControlBlock ");
        values.Append("\n");
    }
    */

    /* 1.203 prohibited.
     * 
    Echo("Accepted resources:");
    values.Append("\nAccepted resources:");
    MyResourceSinkComponent sink;
    unit.Components.TryGet<MyResourceSinkComponent>(out sink);
    if (sink != null)
    {
        var list = sink.AcceptedResources;
        for (int j = 0; j < list.Count; ++j)
        {
            values.Append("\n " + list[j].SubtypeId.ToString() +" ("+list[j].SubtypeName+")");
            Echo(list[j].SubtypeId.ToString() +" ("+list[j].SubtypeName+")");

            float currentInput = 0;
            float maxRequiredInput = 0;
            bool isPoweredBy = false;

            currentInput=sink.CurrentInputByType(list[j]);
            isPoweredBy=sink.IsPoweredByType(list[j]);
            maxRequiredInput=sink.MaxRequiredInputByType(list[j]);
//            float available = sink.ResourceAvailableByType(list[j]); // Prohibited


            values.Append("\n Current=" + currentInput.ToString() + " Max=" + maxRequiredInput.ToString() + " PoweredBy=" + isPoweredBy.ToString()
//                + " Available=" +available.ToString()
                );

        }
    }
    else
    {
        values.Append("\n No Resources");
        Echo("No resources");
    }
    Echo("Provided resources:");
    values.Append("\nProvided resources:");
    MyResourceSourceComponent source;
    unit.Components.TryGet<MyResourceSourceComponent>(out source);

    if (source != null)
    {
        var list = source.ResourceTypes;
        for (int j = 0; j < list.Count; ++j)
        {
            
            list[j].SubtypeId.ToString());
            Echo(list[j].SubtypeId.ToString());
            float maxoutput = source.DefinedOutputByType(list[j]);
            float currentoutput = source.CurrentOutputByType(list[j]);
            values.Append(" Current=" + currentoutput + " Max=" + maxoutput);
        }
    }
    else
    {
        values.Append("\n No Resources");
        Echo("No resources");
    }
    */
    values.Append("\n-------------\n");
}

