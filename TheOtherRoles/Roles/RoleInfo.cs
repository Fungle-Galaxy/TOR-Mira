using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles;

public class RoleInfo
{
    public static Dictionary<RoleId, RoleInfo> roleInfoById = new();
    public Color color;
    public string name { get { return name_ != null ? name_.GetString() : ""; } }
    public string introDescription { get { return introDescription_ != null ? introDescription_.GetString() : ""; } }
    public string shortDescription { get { return shortDescription_ != null ? shortDescription_.GetString() : ""; } }
    public bool isModifier;
    public bool isNeutral;
    TranslationInfo name_ = null;
    TranslationInfo introDescription_ = null;
    TranslationInfo shortDescription_ = null;
    public RoleId roleId;

    public RoleInfo(Color color, RoleId roleId,
        bool isNeutral = false, bool isModifier = false,
        TranslationInfo name = null, TranslationInfo introDescription = null, TranslationInfo shortDescription = null)
    {
        this.color = color;
        this.name_ = name != null ? name : ModTranslation.GetRoleName(roleId, color);
        this.introDescription_ = introDescription != null ? introDescription : ModTranslation.GetRoleIntroDesc(roleId, color);
        this.shortDescription_ = shortDescription != null ? shortDescription : ModTranslation.GetRoleShortDesc(roleId, color);
        this.roleId = roleId;
        this.isNeutral = isNeutral;
        this.isModifier = isModifier;
        roleInfoById.TryAdd(roleId, this);
    }

    public bool isImpostor => color == Palette.ImpostorRed && !(roleId == RoleId.Spy);
}
