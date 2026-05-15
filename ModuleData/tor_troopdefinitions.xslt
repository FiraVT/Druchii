<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="xml" omit-xml-declaration="no" indent="yes" />

    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <!-- Remove duplicate druchii troops already in TOR_Core -->
    <xsl:template match="NPCCharacter[@id='tor_de_conscript']" />
    <xsl:template match="NPCCharacter[@id='tor_de_bleaksword']" />
    <xsl:template match="NPCCharacter[@id='tor_de_dreadspear']" />
    <xsl:template match="NPCCharacter[@id='tor_de_darkshard']" />
    <xsl:template match="NPCCharacter[@id='tor_de_darkrider']" />

</xsl:stylesheet>
