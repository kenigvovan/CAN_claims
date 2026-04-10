namespace claims.src.database
{
    public static class QuerryTemplates
    {

        //CITY
        public static readonly string DELETE_CITY = "DELETE FROM CITIES WHERE guid = @guid";
        public static readonly string INSERT_CITY = "INSERT INTO CITIES (NAME, MAYOR, GUID, TIMESTAMPCREATED, debtbalance, perm, plotgroups, prisons, alliance, defaultplotcost, hostiles, comrades, invmsg, opencity, fee, criminals, istechnical, bonusplots, extrachunksbought, citycolor, templerespawnpoints, ranks) VALUES (@name, @mayor, @guid, @timestampcreated, @debtbalance,@perm,@plotgroups,@prisons, @alliance, @defaultplotcost, @hostiles, @comrades, @invmsg, @opencity, @fee, @criminals, @istechnical, @bonusplots, @extrachunksbought, @citycolor, @templerespawnpoints, @ranks)";
        public static readonly string UPDATE_CITY = "UPDATE CITIES SET NAME=@name, MAYOR=@mayor, GUID=@guid, timestampcreated=@timestampcreated, debtbalance=@debtbalance, perm=@perm, plotgroups=@plotgroups, prisons=@prisons, alliance=@alliance, defaultplotcost=@defaultplotcost, hostiles=@hostiles, comrades=@comrades, invmsg=@invmsg, opencity=@opencity, fee=@fee, criminals=@criminals, istechnical=@istechnical, bonusplots=@bonusplots, extrachunksbought=@extrachunksbought, citycolor=@citycolor, templerespawnpoints=@templerespawnpoints, ranks=@ranks WHERE guid=@guid";

        //ALLIANCE
        public static readonly string DELETE_ALLIANCE = "DELETE FROM ALLIANCIES WHERE guid=@guid";
        public static readonly string INSERT_ALLIANCE = "INSERT INTO ALLIANCIES (name, guid,maincity,cities,hostiles,comrades,alliancefee,neutral, prefix, timestampcreated) VALUES (@name, @guid, @maincity, @cities, @hostiles ,@comrades, @alliancefee ,@neutral, @prefix, @timestampcreated)";
        public static readonly string UPDATE_ALLIANCE = "UPDATE ALLIANCIES SET name=@name, guid=@guid, maincity=@maincity, cities=@cities, hostiles=@hostiles, comrades=@comrades ,alliancefee=@alliancefee ,neutral=@neutral, prefix=@prefix , timestampcreated=@timestampcreated where guid=@guid";

        //PLAYER
        public static readonly string DELETE_PLAYER = "DELETE FROM PLAYERS WHERE UID=@uid";
        public static readonly string INSERT_PLAYER = "INSERT INTO PLAYERS (NAME, UID,timestampfirstjoined, timestamplastonline, comrades, city, citytitles, title, aftername, perms, prisonguid, prisonhoursleft) VALUES (@name, @uid, @timestampfirstjoined, @timestamplastonline, @comrades,@city,@citytitles,@title,@aftername,@perms,@prisonguid,@prisonhoursleft)";
        public static readonly string UPDATE_PLAYER = "UPDATE PLAYERS SET NAME=@name, UID=@uid, timestampfirstjoined=@timestampfirstjoined, timestamplastonline=@timestamplastonline, comrades=@comrades, city=@city, citytitles=@citytitles, title=@title, aftername=@aftername, perms=@perms,prisonguid=@prisonguid, prisonhoursleft=@prisonhoursleft  WHERE UID=@uid";

        //PLOTGROUP
        public static readonly string DELETE_CITYPLOTGROUP = "DELETE FROM CITYPLOTSGROUP WHERE guid=@guid";
        public static readonly string INSERT_CITYPLOTGROUP = "INSERT INTO CITYPLOTSGROUP (name, guid, perms, players, plotsgroupfee, city) VALUES (@name, @guid, @perms,@players, @plotsgroupfee, @city)";
        public static readonly string UPDATE_CITYPLOTGROUP = "UPDATE CITYPLOTSGROUP SET name=@name, guid=@guid, perms=@perms, players=@players, plotsgroupfee=@plotsgroupfee, city=@city where guid=@guid";

        //PRISON
        public static readonly string DELETE_PRISON = "DELETE FROM PRISONS WHERE guid=@guid";
        public static readonly string INSERT_PRISON = "INSERT INTO PRISONS (name, guid, prisonCells, city, x,z) VALUES (@name,@guid,@prisonCells, @city,@x,@z)";
        public static readonly string UPDATE_PRISON = "UPDATE PRISONS SET name=@name, guid=@guid, prisonCells=@prisonCells, city=@city, x=@x,z=@z where guid=@guid";

        //WORLD
        public static readonly string DELETE_WORLD = "DELETE FROM WORLDS WHERE guid=@guid";
        public static readonly string INSERT_WORLD = "INSERT INTO WORLDS (name, guid,pvpeverywhere,fireeverywhere,blasteverywhere,fireforbidden,pvpforbidden,blastforbidden) VALUES (@name,@guid,@pvpeverywhere,@fireeverywhere,@blasteverywhere,@fireforbidden,@pvpforbidden,@blastforbidden)";
        public static readonly string UPDATE_WORLD = "UPDATE WORLDS SET name=@name, guid=@guid, pvpeverywhere=@pvpeverywhere, fireeverywhere=@fireeverywhere,blasteverywhere=@blasteverywhere,fireforbidden=@fireforbidden,pvpforbidden=@pvpforbidden,blastforbidden=@blastforbidden where guid=@guid";

        //PLOT
        public static readonly string DELETE_PLOT = "DELETE FROM PLOTS WHERE x=@x AND z=@z";
        public static readonly string INSERT_PLOT = "INSERT INTO PLOTS (name, x,z,city,ownerofplot,type,price,customtax,perms,plotgroupguid, markednopvp, plotdesc, extraBought, wascaptured)" +
                                                    " VALUES (@name,@x,@z,@city,@ownerofplot,@type,@price,@customtax,@perms,@plotgroupguid, @markednopvp, @plotdesc, @extraBought, @wascaptured)";
        public static readonly string UPDATE_PLOT = "UPDATE PLOTS SET name=@name, x=@x,z=@z,city=@city, ownerofplot=@ownerofplot, type=@type,price=@price,customtax=@customtax,perms=@perms, plotgroupguid=@plotgroupguid, markednopvp=@markednopvp, plotdesc=@plotdesc, extraBought=@extraBought, wascaptured=@wascaptured" +
                                                    " where x=@x and z=@z";

        //CONFLICT
        public static readonly string DELETE_CONFLICT = "DELETE FROM CONFLICTS WHERE guid=@guid";
        public static readonly string INSERT_CONFLICT = "INSERT INTO CONFLICTS (name, guid, firstside, secondside, conflictstate, startedby, warranges, minimumdaysbetweenbattles, lastbattledatestart, lastbattledateend, timestampstarted, firstwarranges, secondwarranges, nextbattledatestart, nextbattledateend, firstside_type, secondside_type, startedby_type) VALUES (@name,@guid,@firstside,@secondside,@conflictstate, @startedby, @warranges, @minimumdaysbetweenbattles, @lastbattledatestart, @lastbattledateend, @timestampstarted, @firstwarranges, @secondwarranges, @nextbattledatestart, @nextbattledateend, @firstside_type, @secondside_type, @startedby_type)";
        public static readonly string UPDATE_CONFLICT = "UPDATE CONFLICTS  SET name=@name, guid=@guid, firstside=@firstside, secondside=@secondside, conflictstate=@conflictstate, startedby=@startedby, warranges=@warranges, minimumdaysbetweenbattles=@minimumdaysbetweenbattles, lastbattledatestart=@lastbattledatestart, lastbattledateend=@lastbattledateend, timestampstarted=@timestampstarted, firstwarranges=@firstwarranges, secondwarranges=@secondwarranges, nextbattledatestart=@nextbattledatestart, nextbattledateend=@nextbattledateend, firstside_type=@firstside_type, secondside_type=@secondside_type, startedby_type=@startedby_type where guid=@guid";
    }
}
