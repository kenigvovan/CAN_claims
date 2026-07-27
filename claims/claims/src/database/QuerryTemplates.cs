using System.Collections.Generic;

namespace claims.src.database
{
    public static class QuerryTemplates
    {

        //CITY
        public static readonly string DELETE_CITY = "DELETE FROM CITIES WHERE guid = @guid";
        public static readonly string INSERT_CITY = "INSERT INTO CITIES (NAME, MAYOR, GUID, TIMESTAMPCREATED, debtbalance, perm, plotgroups, prisons, alliance, defaultplotcost, hostiles, comrades, invmsg, opencity, fee, criminals, istechnical, bonusplots, extrachunksbought, citycolor, templerespawnpoints, ranks, eventlog, warcooldowns, grievances, overlord, vassals, vassalsince, naps, warjustifications) VALUES (@name, @mayor, @guid, @timestampcreated, @debtbalance,@perm,@plotgroups,@prisons, @alliance, @defaultplotcost, @hostiles, @comrades, @invmsg, @opencity, @fee, @criminals, @istechnical, @bonusplots, @extrachunksbought, @citycolor, @templerespawnpoints, @ranks, @eventlog, @warcooldowns, @grievances, @overlord, @vassals, @vassalsince, @naps, @warjustifications)";
        public static readonly string UPDATE_CITY = "UPDATE CITIES SET NAME=@name, MAYOR=@mayor, GUID=@guid, timestampcreated=@timestampcreated, debtbalance=@debtbalance, perm=@perm, plotgroups=@plotgroups, prisons=@prisons, alliance=@alliance, defaultplotcost=@defaultplotcost, hostiles=@hostiles, comrades=@comrades, invmsg=@invmsg, opencity=@opencity, fee=@fee, criminals=@criminals, istechnical=@istechnical, bonusplots=@bonusplots, extrachunksbought=@extrachunksbought, citycolor=@citycolor, templerespawnpoints=@templerespawnpoints, ranks=@ranks, eventlog=@eventlog, warcooldowns=@warcooldowns, grievances=@grievances, overlord=@overlord, vassals=@vassals, vassalsince=@vassalsince, naps=@naps, warjustifications=@warjustifications WHERE guid=@guid";

        //ALLIANCE
        public static readonly string DELETE_ALLIANCE = "DELETE FROM ALLIANCIES WHERE guid=@guid";
        public static readonly string INSERT_ALLIANCE = "INSERT INTO ALLIANCIES (name, guid,maincity,cities,hostiles,comrades,alliancefee,neutral, prefix, timestampcreated, pendingunionbreaks, unionbreakcooldowns) VALUES (@name, @guid, @maincity, @cities, @hostiles ,@comrades, @alliancefee ,@neutral, @prefix, @timestampcreated, @pendingunionbreaks, @unionbreakcooldowns)";
        public static readonly string UPDATE_ALLIANCE = "UPDATE ALLIANCIES SET name=@name, guid=@guid, maincity=@maincity, cities=@cities, hostiles=@hostiles, comrades=@comrades ,alliancefee=@alliancefee ,neutral=@neutral, prefix=@prefix , timestampcreated=@timestampcreated, pendingunionbreaks=@pendingunionbreaks, unionbreakcooldowns=@unionbreakcooldowns where guid=@guid";

        //PLAYER
        public static readonly string DELETE_PLAYER = "DELETE FROM PLAYERS WHERE UID=@uid";
        public static readonly string INSERT_PLAYER = "INSERT INTO PLAYERS (NAME, UID,timestampfirstjoined, timestamplastonline, comrades, city, citytitles, title, aftername, perms, prisonguid, prisonhoursleft, bounties) VALUES (@name, @uid, @timestampfirstjoined, @timestamplastonline, @comrades,@city,@citytitles,@title,@aftername,@perms,@prisonguid,@prisonhoursleft,@bounties)";
        public static readonly string UPDATE_PLAYER = "UPDATE PLAYERS SET NAME=@name, UID=@uid, timestampfirstjoined=@timestampfirstjoined, timestamplastonline=@timestamplastonline, comrades=@comrades, city=@city, citytitles=@citytitles, title=@title, aftername=@aftername, perms=@perms,prisonguid=@prisonguid, prisonhoursleft=@prisonhoursleft, bounties=@bounties  WHERE UID=@uid";

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
        public static readonly string INSERT_PLOT = "INSERT INTO PLOTS (name, x,z,city,ownerofplot,type,price,customtax,perms,plotgroupguid, markednopvp, plotdesc, extraBought, wascaptured, timestampclaimed, lastpaidprice)" +
                                                    " VALUES (@name,@x,@z,@city,@ownerofplot,@type,@price,@customtax,@perms,@plotgroupguid, @markednopvp, @plotdesc, @extraBought, @wascaptured, @timestampclaimed, @lastpaidprice)";
        public static readonly string UPDATE_PLOT = "UPDATE PLOTS SET name=@name, x=@x,z=@z,city=@city, ownerofplot=@ownerofplot, type=@type,price=@price,customtax=@customtax,perms=@perms, plotgroupguid=@plotgroupguid, markednopvp=@markednopvp, plotdesc=@plotdesc, extraBought=@extraBought, wascaptured=@wascaptured, timestampclaimed=@timestampclaimed, lastpaidprice=@lastpaidprice" +
                                                    " where x=@x and z=@z";

        //CONFLICT
        public static readonly string DELETE_UNIONLETTER = "DELETE FROM UNIONLETTERS WHERE guid=@guid";
        public static readonly string INSERT_UNIONLETTER = "INSERT INTO UNIONLETTERS (guid, fromside, toside, purpose, timestampexpire) VALUES (@guid, @fromside, @toside, @purpose, @timestampexpire)";
        public static readonly string UPDATE_UNIONLETTER = "UPDATE UNIONLETTERS SET fromside=@fromside, toside=@toside, purpose=@purpose, timestampexpire=@timestampexpire where guid=@guid";

        public static readonly string DELETE_CONFLICTLETTER = "DELETE FROM CONFLICTLETTERS WHERE guid=@guid";
        public static readonly string INSERT_CONFLICTLETTER = "INSERT INTO CONFLICTLETTERS (guid, fromside, fromside_type, toside, toside_type, purpose, timestampexpire, termtype, termamount, termplotx, termplotz, termhasplot, napdays) VALUES (@guid, @fromside, @fromside_type, @toside, @toside_type, @purpose, @timestampexpire, @termtype, @termamount, @termplotx, @termplotz, @termhasplot, @napdays)";
        public static readonly string UPDATE_CONFLICTLETTER = "UPDATE CONFLICTLETTERS SET fromside=@fromside, fromside_type=@fromside_type, toside=@toside, toside_type=@toside_type, purpose=@purpose, timestampexpire=@timestampexpire, termtype=@termtype, termamount=@termamount, termplotx=@termplotx, termplotz=@termplotz, termhasplot=@termhasplot, napdays=@napdays where guid=@guid";

        public static readonly string DELETE_CONFLICT = "DELETE FROM CONFLICTS WHERE guid=@guid";
        public static readonly string INSERT_CONFLICT = "INSERT INTO CONFLICTS (name, guid, firstside, secondside, conflictstate, startedby, warranges, minimumdaysbetweenbattles, lastbattledatestart, lastbattledateend, timestampstarted, firstwarranges, secondwarranges, nextbattledatestart, nextbattledateend, firstside_type, secondside_type, startedby_type, firstscore, secondscore, firstplotscaptured, secondplotscaptured, firstkills, secondkills, firstpillaged, secondpillaged) VALUES (@name,@guid,@firstside,@secondside,@conflictstate, @startedby, @warranges, @minimumdaysbetweenbattles, @lastbattledatestart, @lastbattledateend, @timestampstarted, @firstwarranges, @secondwarranges, @nextbattledatestart, @nextbattledateend, @firstside_type, @secondside_type, @startedby_type, @firstscore, @secondscore, @firstplotscaptured, @secondplotscaptured, @firstkills, @secondkills, @firstpillaged, @secondpillaged)";
        public static readonly string UPDATE_CONFLICT = "UPDATE CONFLICTS  SET name=@name, guid=@guid, firstside=@firstside, secondside=@secondside, conflictstate=@conflictstate, startedby=@startedby, warranges=@warranges, minimumdaysbetweenbattles=@minimumdaysbetweenbattles, lastbattledatestart=@lastbattledatestart, lastbattledateend=@lastbattledateend, timestampstarted=@timestampstarted, firstwarranges=@firstwarranges, secondwarranges=@secondwarranges, nextbattledatestart=@nextbattledatestart, nextbattledateend=@nextbattledateend, firstside_type=@firstside_type, secondside_type=@secondside_type, startedby_type=@startedby_type, firstscore=@firstscore, secondscore=@secondscore, firstplotscaptured=@firstplotscaptured, secondplotscaptured=@secondplotscaptured, firstkills=@firstkills, secondkills=@secondkills, firstpillaged=@firstpillaged, secondpillaged=@secondpillaged where guid=@guid";

        /// <summary>
        /// Table name -> its three statements. The insert/update/delete paths all look the row up
        /// here, so a newly added table cannot be wired into one operation and silently forgotten in
        /// another (which used to mean a row that saves but never deletes).
        /// Declared last on purpose: static fields initialise in declaration order, so the statements
        /// it references must already be assigned.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, (string Insert, string Update, string Delete)> ByTable =
            new Dictionary<string, (string Insert, string Update, string Delete)>
            {
                { "CITIES",          (INSERT_CITY,           UPDATE_CITY,           DELETE_CITY) },
                { "PLAYERS",         (INSERT_PLAYER,         UPDATE_PLAYER,         DELETE_PLAYER) },
                { "CITYPLOTSGROUP",  (INSERT_CITYPLOTGROUP,  UPDATE_CITYPLOTGROUP,  DELETE_CITYPLOTGROUP) },
                { "PRISONS",         (INSERT_PRISON,         UPDATE_PRISON,         DELETE_PRISON) },
                { "WORLDS",          (INSERT_WORLD,          UPDATE_WORLD,          DELETE_WORLD) },
                { "PLOTS",           (INSERT_PLOT,           UPDATE_PLOT,           DELETE_PLOT) },
                { "ALLIANCIES",      (INSERT_ALLIANCE,       UPDATE_ALLIANCE,       DELETE_ALLIANCE) },
                { "CONFLICTS",       (INSERT_CONFLICT,       UPDATE_CONFLICT,       DELETE_CONFLICT) },
                { "CONFLICTLETTERS", (INSERT_CONFLICTLETTER, UPDATE_CONFLICTLETTER, DELETE_CONFLICTLETTER) },
                { "UNIONLETTERS",    (INSERT_UNIONLETTER,    UPDATE_UNIONLETTER,    DELETE_UNIONLETTER) },
            };
    }
}
