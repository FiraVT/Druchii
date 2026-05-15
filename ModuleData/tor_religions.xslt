<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="xml" omit-xml-declaration="no" indent="yes" />

    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="Religions">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
            <Religion id="cult_of_khaine" Name="{{=str_tor_religion_cult_khaine}}Cult of Khaine" Culture="Culture.druchii" Pantheon="Elven" DeityName="{{=str_tor_deity_khaine}}Khaine">
                <HostileReligions />
                <Followers>
                    <Follower id="Faction.druchii_clan_1" />
                    <Follower id="Faction.druchii_clan_2" />
                    <Follower id="Faction.druchii_clan_3" />
                </Followers>
                <ReligiousTroops>
                    <ReligiousTroop id="NPCCharacter.tor_de_bleaksword" />
                    <ReligiousTroop id="NPCCharacter.tor_de_dreadspear" />
                    <ReligiousTroop id="NPCCharacter.tor_de_darkshard" />
                    <ReligiousTroop id="NPCCharacter.tor_de_darkshard_shield" />
                </ReligiousTroops>
                <EliteUnits>
                    <EliteUnit id="NPCCharacter.tor_de_black_guard" />
                    <EliteUnit id="NPCCharacter.tor_de_har_ganeth_executioner" />
                    <EliteUnit id="NPCCharacter.tor_de_sister_of_slaughter" />
                    <EliteUnit id="NPCCharacter.tor_de_doomfire_warlocks" />
                </EliteUnits>
                <ReligiousArtifacts />
            </Religion>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
