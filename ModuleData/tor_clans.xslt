<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="xml" omit-xml-declaration="no" indent="yes" />

    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <!-- Override druchii_clan_1 to be a noble sovereign faction instead of outlaws -->
    <xsl:template match="Faction[@id='druchii_clan_1']">
        <xsl:copy>
            <!-- Copy all attributes except the ones we want to change/ensure -->
            <xsl:for-each select="@*">
                <xsl:if test="name() != 'name' and name() != 'is_minor_faction' and name() != 'is_outlaw' and name() != 'is_noble' and name() != 'tier' and name() != 'super_faction' and name() != 'is_bandit'">
                    <xsl:copy-of select="."/>
                </xsl:if>
            </xsl:for-each>
            
            <!-- Set our desired attributes -->
            <xsl:attribute name="name">{{=str_clan_name_druchii_1}}House of Naggarond</xsl:attribute>
            <xsl:attribute name="is_minor_faction">false</xsl:attribute>
            <xsl:attribute name="is_outlaw">false</xsl:attribute>
            <xsl:attribute name="is_noble">true</xsl:attribute>
            <xsl:attribute name="tier">6</xsl:attribute>
            <xsl:attribute name="super_faction">Kingdom.druchii_kingdom</xsl:attribute>
            <xsl:attribute name="default_party_template">PartyTemplate.druchii_slaver_party</xsl:attribute>
            
            <!-- Apply templates to children to preserve/transform them -->
            <xsl:apply-templates select="node()"/>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
