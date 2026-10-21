using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots.auction;
using claims.src.rights;
using claims.src.subplots.geometry;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace claims.src.subplots
{
    /// <summary>
    /// Everything /subplot changes, judged and applied here so that the commands only resolve their
    /// arguments and the tests need no server. Each call answers with the result the player sees.
    /// The city's managers (CITY_SUBPLOT_MANAGE) shape, hand out and remove sub-plots; the owner may
    /// also choose who shares theirs and what the groups may do in it.
    /// </summary>
    public static class SubPlotEditor
    {
        public static bool CanManage(PlayerInfo actor, City city)
        {
            return actor != null && city != null && actor.hasCity() && actor.City.Equals(city)
                && actor.PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE);
        }

        public static bool CanEdit(PlayerInfo actor, SubPlot subPlot)
        {
            return subPlot != null && (CanManage(actor, subPlot.City) || (subPlot.Owner != null && subPlot.Owner.Equals(actor)));
        }

        public static IEnumerable<SubPlot> OfCity(City city)
        {
            if (claims.dataStorage == null || city == null) return Enumerable.Empty<SubPlot>();
            return claims.dataStorage.AllSubPlots().Where(s => city.Equals(s.City));
        }

        /// <summary>Whether the player owns as many sub-plots as one may, anywhere.</summary>
        public static bool AtOwnedLimit(PlayerInfo player)
        {
            int max = claims.config.SUBPLOT_MAX_OWNED_PER_PLAYER;
            return max > 0 && claims.dataStorage.AllSubPlots().Count(s => player.Equals(s.Owner)) >= max;
        }

        public static SubPlot FindByName(City city, string name)
        {
            return OfCity(city).FirstOrDefault(s => string.Equals(s.GetPartName(), name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Why the shape cannot be <paramref name="self"/>'s (or a new sub-plot's when null) in this
        /// city, as a lang key; null when it can.
        /// </summary>
        public static string Validate(BoxShape shape, City city, SubPlot self)
        {
            string problem = SubPlotRules.Check(shape, city, PlotAt,
                claims.dataStorage.AllSubPlots().Where(s => s != self),
                claims.config.SUBPLOT_MAX_BOXES, PlotPosition.plotSize, SubPlot.WorldHeight);
            if (problem != null) return problem;
            // With the limit in the text: the player has to know what would fit. Lang.Get of an
            // already translated text returns it unchanged, so callers treat it as a key.
            int max = claims.config.SUBPLOT_MAX_VOLUME, min = claims.config.SUBPLOT_MIN_VOLUME;
            if (max > 0 && shape.Volume > max) return Vintagestory.API.Config.Lang.Get("claims:subplot_too_big", shape.Volume, max);
            if (min > 0 && shape.Volume < min) return Vintagestory.API.Config.Lang.Get("claims:subplot_too_small", shape.Volume, min);
            // An offer to other cities promises the whole chunk.
            foreach (Vec2i chunk in SubPlot.ChunksOf(shape, PlotPosition.plotSize))
            {
                Plot plot = PlotAt(chunk);
                if (plot != null && AuctionRegistry.TryGetRunningFor(plot, out _)) return "claims:subplot_on_plot_listed";
            }
            return null;
        }

        private static Plot PlotAt(Vec2i chunk)
        {
            claims.dataStorage.GetPlot(new PlotPosition(chunk.X, chunk.Y), out Plot plot);
            return plot;
        }

        public static TextCommandResult Create(PlayerInfo actor, string rawName, BlockBox box)
        {
            City city = actor?.City;
            if (!CanManage(actor, city)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            string name = Filter.filterName(rawName ?? "");
            if (string.IsNullOrEmpty(name)) return TextCommandResult.Error("claims:subplot_bad_name");
            if (FindByName(city, name) != null) return Error("claims:subplot_name_taken", name);
            int perCity = claims.config.SUBPLOT_MAX_PER_CITY;
            if (perCity > 0 && OfCity(city).Count() >= perCity) return Error("claims:subplot_city_limit", perCity);

            var shape = new BoxShape(box);
            string problem = Validate(shape, city, null);
            if (problem != null) return TextCommandResult.Error(problem);

            var subPlot = new SubPlot(name, Guid.NewGuid().ToString()) { City = city, Shape = shape };
            claims.dataStorage.AddSubPlot(subPlot);
            subPlot.saveToDatabase(false);
            SubPlotSync.Log(subPlot, citylog.EnumCityLogEvent.SubPlotCreated);
            return Success("claims:subplot_created", name, shape.Volume);
        }

        /// <summary>Adds the box to the shape; the part already inside it is left as it is.</summary>
        public static TextCommandResult AddBox(PlayerInfo actor, SubPlot subPlot, BlockBox box)
        {
            if (!CanManage(actor, subPlot?.City)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            BoxShape shape = ShapeWithBox(subPlot, box, out string problem);
            if (shape == null) return Error(problem, subPlot.GetPartName());
            return Reshape(subPlot, shape, "claims:subplot_box_added");
        }

        /// <summary>Takes the box out of the shape. Taking out everything is what delete is for.</summary>
        public static TextCommandResult CutBox(PlayerInfo actor, SubPlot subPlot, BlockBox box)
        {
            if (!CanManage(actor, subPlot?.City)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            BoxShape shape = ShapeWithoutBox(subPlot, box, out string problem);
            if (shape == null) return Error(problem, subPlot.GetPartName());
            return Reshape(subPlot, shape, "claims:subplot_box_cut");
        }

        /// <summary>
        /// The sub-plot's shape with the box added - only the part not inside it already - or null with
        /// the reason when there is nothing to add.
        /// </summary>
        public static BoxShape ShapeWithBox(SubPlot subPlot, BlockBox box, out string problem)
        {
            problem = null;
            var pieces = new List<BlockBox> { box };
            foreach (BlockBox existing in subPlot.Shape.Boxes)
            {
                pieces = pieces.SelectMany(p => p.Subtract(existing)).ToList();
            }
            if (pieces.Count == 0)
            {
                problem = "claims:subplot_box_already_inside";
                return null;
            }
            return new BoxShape(subPlot.Shape.Boxes.Concat(pieces));
        }

        /// <summary>The sub-plot's shape with the box taken out, or null with the reason when that cannot be.</summary>
        public static BoxShape ShapeWithoutBox(SubPlot subPlot, BlockBox box, out string problem)
        {
            problem = null;
            if (!subPlot.Shape.Intersects(box))
            {
                problem = "claims:subplot_box_outside";
                return null;
            }
            BoxShape shape = subPlot.Shape.Without(box);
            if (shape == null) problem = "claims:subplot_cut_everything";
            return shape;
        }

        /// <summary>
        /// What would stop the box going into the sub-plot (<paramref name="add"/>) or out of it; null
        /// when it would go through. The same judgement as AddBox and CutBox, rights aside.
        /// </summary>
        public static string WhyNotChange(SubPlot subPlot, BlockBox box, bool add)
        {
            BoxShape shape = add ? ShapeWithBox(subPlot, box, out string problem) : ShapeWithoutBox(subPlot, box, out problem);
            return shape == null ? problem : Validate(shape, subPlot.City, subPlot);
        }

        private static TextCommandResult Reshape(SubPlot subPlot, BoxShape shape, string doneKey)
        {
            string problem = Validate(shape, subPlot.City, subPlot);
            if (problem != null) return TextCommandResult.Error(problem);
            claims.dataStorage.ReshapeSubPlot(subPlot, shape);
            subPlot.saveToDatabase();
            return Success(doneKey, subPlot.GetPartName(), shape.Volume);
        }

        /// <summary>Removes the sub-plot; an owner gets back what they paid, as when the city loses the ground.</summary>
        public static TextCommandResult Delete(PlayerInfo actor, SubPlot subPlot)
        {
            if (!CanManage(actor, subPlot?.City)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            SubPlotLifecycle.Dissolve(subPlot, "claims:subplot_removed_by_city", "claims:subplot_removed_by_city_refund");
            return Success("claims:subplot_deleted", subPlot.GetPartName());
        }

        /// <summary>Hands the sub-plot to a citizen, or back to the city with null. Members and the price go.</summary>
        public static TextCommandResult SetOwner(PlayerInfo actor, SubPlot subPlot, PlayerInfo newOwner)
        {
            if (!CanManage(actor, subPlot?.City)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            if (newOwner != null && !(newOwner.hasCity() && newOwner.City.Equals(subPlot.City)))
            {
                return Error("claims:subplot_owner_not_citizen", newOwner.GetPartName());
            }
            if (newOwner != null && !newOwner.Equals(subPlot.Owner) && AtOwnedLimit(newOwner))
            {
                return Error("claims:subplot_owner_limit", newOwner.GetPartName(), claims.config.SUBPLOT_MAX_OWNED_PER_PLAYER);
            }
            // A raise announced to the previous owner is theirs to accept, not the new one's.
            SubPlotLifecycle.ReturnToCity(subPlot);
            if (newOwner != null)
            {
                subPlot.Owner = newOwner;
                // Given, not bought: nothing to refund later.
                subPlot.TimeStampClaimed = TimeFunctions.getEpochSeconds();
                Changed(subPlot);
                SubPlotSync.NotifyPayment(newOwner);
                SubPlotSync.Log(subPlot, citylog.EnumCityLogEvent.SubPlotGiven, newOwner.GetPartName());
            }
            return newOwner == null
                ? Success("claims:subplot_owner_cleared", subPlot.GetPartName())
                : Success("claims:subplot_owner_set", subPlot.GetPartName(), newOwner.GetPartName());
        }

        public static TextCommandResult AddMember(PlayerInfo actor, SubPlot subPlot, PlayerInfo member)
        {
            if (!CanEdit(actor, subPlot)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            if (member == null) return TextCommandResult.Error("claims:no_such_player");
            if (member.Equals(subPlot.Owner)) return Error("claims:subplot_member_is_owner", member.GetPartName());
            if (!subPlot.Members.Add(member)) return Error("claims:subplot_member_already", member.GetPartName());
            Changed(subPlot);
            return Success("claims:subplot_member_added", member.GetPartName(), subPlot.GetPartName());
        }

        public static TextCommandResult RemoveMember(PlayerInfo actor, SubPlot subPlot, PlayerInfo member)
        {
            if (!CanEdit(actor, subPlot)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            if (member == null || !subPlot.Members.Remove(member)) return Error("claims:subplot_member_not_found", member?.GetPartName() ?? "");
            Changed(subPlot);
            return Success("claims:subplot_member_removed", member.GetPartName(), subPlot.GetPartName());
        }

        /// <summary>group: citizen, stranger, ally, friend; type: use, build, attack; state: on/off.</summary>
        public static TextCommandResult SetPerm(PlayerInfo actor, SubPlot subPlot, string group, string type, string state)
        {
            if (!CanEdit(actor, subPlot)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            if (!subPlot.PermsHandler.setAccessPerm(group, type, state)) return TextCommandResult.Error("claims:wrong_value");
            Changed(subPlot);
            return Success("claims:subplot_perm_set", subPlot.GetPartName(), group, type, state);
        }

        /// <summary>
        /// Puts the sub-plot into a plots group of its city, or out of any with null: the group's
        /// members get the group's rights inside, and the group counts it as its land for the fee.
        /// </summary>
        public static TextCommandResult SetGroup(PlayerInfo actor, SubPlot subPlot, string groupName)
        {
            if (!CanManage(actor, subPlot?.City)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            CityPlotsGroup group = null;
            if (groupName != null)
            {
                group = subPlot.City.getCityPlotsGroups()
                    .FirstOrDefault(g => string.Equals(g.GetPartName(), groupName, StringComparison.OrdinalIgnoreCase));
                if (group == null) return TextCommandResult.Error("claims:no_such_group_found");
            }
            if (subPlot.Group == group) return Success("claims:subplot_group_unchanged", subPlot.GetPartName());
            subPlot.Group = group;
            Changed(subPlot);
            return group == null
                ? Success("claims:subplot_group_cleared", subPlot.GetPartName())
                : Success("claims:subplot_group_set", subPlot.GetPartName(), group.GetPartName());
        }

        /// <summary>
        /// The usual rule sets in one go, the same for every group: "private" - nothing; "shop" -
        /// use only, doors and counters but not the walls; "open" - everything.
        /// </summary>
        public static TextCommandResult ApplyPreset(PlayerInfo actor, SubPlot subPlot, string preset)
        {
            if (!CanEdit(actor, subPlot)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            bool[] rights = preset switch
            {
                "private" => new[] { false, false, false },
                "shop" => new[] { true, false, false },
                "open" => new[] { true, true, true },
                _ => null
            };
            if (rights == null) return TextCommandResult.Error("claims:wrong_value");
            foreach (bool[] group in new[] { subPlot.PermsHandler.CitizenPerms, subPlot.PermsHandler.StrangerPerms,
                                             subPlot.PermsHandler.AlliancePerms, subPlot.PermsHandler.ComradePerms })
            {
                rights.CopyTo(group, 0);
            }
            Changed(subPlot);
            return Success("claims:subplot_preset_set", subPlot.GetPartName(), Vintagestory.API.Config.Lang.Get("claims:subplot-preset-" + preset));
        }

        /// <summary>Puts a city-held sub-plot up for sale to citizens, or takes it off with a negative price.</summary>
        public static TextCommandResult SetForSale(PlayerInfo actor, SubPlot subPlot, int price)
        {
            if (!CanManage(actor, subPlot?.City)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            if (price < 0)
            {
                subPlot.Price = -1;
                Changed(subPlot);
                return Success("claims:subplot_not_for_sale", subPlot.GetPartName());
            }
            // Villages are outside the economy, as with their plots.
            if (subPlot.City.IsVillage()) return TextCommandResult.Error("claims:village_feature_locked");
            if (subPlot.HasOwner) return Error("claims:subplot_has_owner", subPlot.GetPartName(), subPlot.Owner.GetPartName());
            subPlot.Price = price;
            Changed(subPlot);
            return Success("claims:subplot_for_sale", subPlot.GetPartName(), price);
        }

        /// <summary>Null when <paramref name="buyer"/> may buy the sub-plot now, otherwise why not.</summary>
        public static TextCommandResult CheckCanBuy(PlayerInfo buyer, SubPlot subPlot)
        {
            if (subPlot == null || buyer == null) return TextCommandResult.Error("claims:no_such_player");
            if (!subPlot.IsForSale) return Error("claims:subplot_not_for_sale_now", subPlot.GetPartName());
            if (subPlot.HasOwner) return Error("claims:subplot_has_owner", subPlot.GetPartName(), subPlot.Owner.GetPartName());
            if (!(buyer.hasCity() && buyer.City.Equals(subPlot.City))) return TextCommandResult.Error("claims:cannot_buy_plot_in_another_city");
            if (AtOwnedLimit(buyer)) return Error("claims:subplot_owner_limit", buyer.GetPartName(), claims.config.SUBPLOT_MAX_OWNED_PER_PLAYER);
            if ((decimal)subPlot.Price > claims.economyProvider.GetBalance(buyer.MoneyAccountName)) return TextCommandResult.Error("claims:not_enough_money");
            return null;
        }

        /// <summary>Takes the payment into the city's treasury and hands the sub-plot over; nothing changes if it fails.</summary>
        public static TextCommandResult Buy(PlayerInfo buyer, SubPlot subPlot)
        {
            TextCommandResult refusal = CheckCanBuy(buyer, subPlot);
            if (refusal != null) return refusal;
            long price = (long)subPlot.Price;
            if (price > 0 && claims.economyProvider.Transfer(buyer.MoneyAccountName, subPlot.City.MoneyAccountName, price)
                != economy.MoneyOperationResult.Success)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            subPlot.Owner = buyer;
            subPlot.Members.Clear();
            subPlot.Price = -1;
            subPlot.LastPaidPrice = price;
            subPlot.TimeStampClaimed = TimeFunctions.getEpochSeconds();
            Changed(subPlot);
            SubPlotSync.NotifyPayment(buyer);
            SubPlotSync.Log(subPlot, citylog.EnumCityLogEvent.SubPlotSold, buyer.GetPartName(), price.ToString());
            return Success("claims:subplot_bought", subPlot.GetPartName(), price);
        }

        /// <summary>
        /// What the owner gets back for giving the sub-plot up: the same share of what they paid, and
        /// after the same wait, as for a bought plot; capped at the treasury.
        /// </summary>
        public static long GiveBackRefund(SubPlot subPlot, long now, decimal cityBalance)
        {
            int percent = Math.Clamp(claims.config.PLOT_UNCLAIM_REFUND_PERCENT, 0, 100);
            if (subPlot?.Owner == null || percent <= 0 || subPlot.LastPaidPrice <= 0 || subPlot.TimeStampClaimed == 0) return 0;
            if (now - subPlot.TimeStampClaimed < claims.config.PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS) return 0;
            long wanted = subPlot.LastPaidPrice * percent / 100;
            if (cityBalance <= 0) return 0;
            return cityBalance >= wanted ? wanted : (long)Math.Floor(cityBalance);
        }

        /// <summary>The owner gives the sub-plot back to the city. A failed refund keeps it theirs.</summary>
        public static TextCommandResult GiveBack(PlayerInfo actor, SubPlot subPlot)
        {
            if (subPlot?.Owner == null || !subPlot.Owner.Equals(actor)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            string cityAccount = subPlot.City.MoneyAccountName;
            long refund = GiveBackRefund(subPlot, TimeFunctions.getEpochSeconds(), claims.economyProvider.GetBalance(cityAccount));
            if (refund > 0 && claims.economyProvider.Transfer(cityAccount, actor.MoneyAccountName, refund)
                != economy.MoneyOperationResult.Success)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            SubPlotLifecycle.ReturnToCity(subPlot);
            return Success("claims:subplot_given_back", subPlot.GetPartName(), refund);
        }

        public static string Describe(SubPlot subPlot)
        {
            string chunks = string.Join(" ", subPlot.Chunks(PlotPosition.plotSize).Select(c => c.X + ":" + c.Y));
            string members = subPlot.Members.Count == 0 ? "-" : string.Join(", ", subPlot.Members.Select(m => m.GetPartName()));
            return Vintagestory.API.Config.Lang.Get("claims:subplot_info", subPlot.GetPartName(),
                subPlot.Owner?.GetPartName() ?? "-", members, subPlot.Shape.Boxes.Count, subPlot.Shape.Volume, chunks,
                subPlot.PermsHandler.ToString(), subPlot.Tax,
                subPlot.IsForSale ? subPlot.Price.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-",
                subPlot.HasPendingTax ? subPlot.PendingTax.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-");
        }

        private static void Changed(SubPlot subPlot)
        {
            subPlot.saveToDatabase();
            SubPlotSync.MarkChanged(subPlot);
        }

        private static TextCommandResult Success(string key, params object[] args) => new TextCommandResult
        {
            Status = EnumCommandStatus.Success, StatusMessage = key, MessageParams = args
        };

        private static TextCommandResult Error(string key, params object[] args) => new TextCommandResult
        {
            Status = EnumCommandStatus.Error, StatusMessage = key, MessageParams = args
        };
    }
}
