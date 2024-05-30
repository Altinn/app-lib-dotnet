using System.Net;

namespace Altinn.App.Core.Tests.Features.Options.Altinn2Provider;

public class Altinn2MetadataApiClientHttpMessageHandlerMoq : HttpMessageHandler
{
    // Instrumentation to test that caching works
    public int CallCounter { get; private set; } = 0;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken
    )
    {
        CallCounter++;

        var url = httpRequestMessage.RequestUri?.ToString() ?? string.Empty;

        if (url.StartsWith("https://www.altinn.no/api/metadata/codelists/serverError"))
        {
            return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError, });
        }

        var stringResult = GetStringResult(url);
        var status = stringResult != null ? HttpStatusCode.OK : HttpStatusCode.NotFound;
        var response = new HttpResponseMessage(status);
        response.Content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(stringResult ?? string.Empty));
        response.Content.Headers.Remove("Content-Type");
        response.Content.Headers.Add("Content-Type", "application/json; charset=utf-8");

        return Task.FromResult(response);
    }

    private static string? GetStringResult(string url)
    {
        return url switch
        {
            "https://www.altinn.no/api/metadata/codelists/ASF_land/2758?language=1033"
                => "{\"Name\":\"ASF_Land\",\"Version\":2758,\"Language\":1033,\"Codes\":[{\"Code\":\"\",\"Value1\":\"\",\"Value2\":\"\",\"Value3\":\"\"},{\"Code\":\"AFGHANISTAN\",\"Value1\":\"AFGHANISTAN\",\"Value2\":\"AF\",\"Value3\":\"404\"},{\"Code\":\"ALBANIA\",\"Value1\":\"ALBANIA\",\"Value2\":\"AL\",\"Value3\":\"111\"},{\"Code\":\"ALGERIE\",\"Value1\":\"ALGERIA\",\"Value2\":\"DZ\",\"Value3\":\"203\"},{\"Code\":\"AM. SAMOA\",\"Value1\":\"AMERICAN SAMOA\",\"Value2\":\"AS\",\"Value3\":\"802\"},{\"Code\":\"ANDORRA\",\"Value1\":\"ANDORRA\",\"Value2\":\"AD\",\"Value3\":\"114\"},{\"Code\":\"ANGOLA\",\"Value1\":\"ANGOLA\",\"Value2\":\"AO\",\"Value3\":\"204\"},{\"Code\":\"ANGUILLA\",\"Value1\":\"ANGUILLA\",\"Value2\":\"AI\",\"Value3\":\"660\"},{\"Code\":\"ANTARKTIS\",\"Value1\":\"ANTARCTICA\",\"Value2\":\"AQ\",\"Value3\":\"901\"},{\"Code\":\"ANTIGUA OG BARBUDA\",\"Value1\":\"ANTIGUA AND BARBUDA\",\"Value2\":\"AG\",\"Value3\":\"603\"},{\"Code\":\"ARGENTINA\",\"Value1\":\"ARGENTINA\",\"Value2\":\"AR\",\"Value3\":\"705\"},{\"Code\":\"ARMENIA\",\"Value1\":\"ARMENIA\",\"Value2\":\"AM\",\"Value3\":\"406\"},{\"Code\":\"ARUBA\",\"Value1\":\"ARUBA\",\"Value2\":\"AW\",\"Value3\":\"657\"},{\"Code\":\"AUSTRALIA\",\"Value1\":\"AUSTRALIA\",\"Value2\":\"AU\",\"Value3\":\"805\"},{\"Code\":\"ØSTERRIKE\",\"Value1\":\"AUSTRIA\",\"Value2\":\"AT\",\"Value3\":\"153\"},{\"Code\":\"AZERBAJDZJAN\",\"Value1\":\"AZERBAIJAN\",\"Value2\":\"AZ\",\"Value3\":\"407\"},{\"Code\":\"BAHAMAS\",\"Value1\":\"BAHAMAS\",\"Value2\":\"BS\",\"Value3\":\"605\"},{\"Code\":\"BAHRAIN\",\"Value1\":\"BAHRAIN\",\"Value2\":\"BH\",\"Value3\":\"409\"},{\"Code\":\"BANGLADESH\",\"Value1\":\"BANGLADESH\",\"Value2\":\"BD\",\"Value3\":\"410\"},{\"Code\":\"BARBADOS\",\"Value1\":\"BARBADOS\",\"Value2\":\"BB\",\"Value3\":\"602\"},{\"Code\":\"HVITERUSSLAND\",\"Value1\":\"BELARUS\",\"Value2\":\"BY\",\"Value3\":\"120\"},{\"Code\":\"BELGIA\",\"Value1\":\"BELGIUM\",\"Value2\":\"BE\",\"Value3\":\"112\"},{\"Code\":\"BELIZE\",\"Value1\":\"BELIZE\",\"Value2\":\"BZ\",\"Value3\":\"604\"},{\"Code\":\"BENIN\",\"Value1\":\"BENIN\",\"Value2\":\"BJ\",\"Value3\":\"229\"},{\"Code\":\"BERMUDA\",\"Value1\":\"BERMUDA\",\"Value2\":\"BM\",\"Value3\":\"606\"},{\"Code\":\"BHUTAN\",\"Value1\":\"BHUTAN\",\"Value2\":\"BT\",\"Value3\":\"412\"},{\"Code\":\"BOLIVIA\",\"Value1\":\"BOLIVIA\",\"Value2\":\"BO\",\"Value3\":\"710\"},{\"Code\":\"BONAIRE, SINT EUSTATIUS OG SABA\",\"Value1\":\"BONAIRE, SINT EUSTATIUS AND SABA\",\"Value2\":\"BQ\",\"Value3\":\"659\"},{\"Code\":\"BOSNIA-HERCEGOVINA\",\"Value1\":\"BOSNIA AND HERZEGOVINA\",\"Value2\":\"BA\",\"Value3\":\"155\"},{\"Code\":\"BOTSWANA\",\"Value1\":\"BOTSWANA\",\"Value2\":\"BW\",\"Value3\":\"205\"},{\"Code\":\"BOUVETØYA\",\"Value1\":\"BOUVET ISLAND\",\"Value2\":\"BV\",\"Value3\":\"904\"},{\"Code\":\"BRASIL\",\"Value1\":\"BRAZIL\",\"Value2\":\"BR\",\"Value3\":\"715\"},{\"Code\":\"BRITISK-INDISKE HAV\",\"Value1\":\"BRITISH INDIAN OCEAN TERRITORY\",\"Value2\":\"IO\",\"Value3\":\"213\"},{\"Code\":\"BRUNEI\",\"Value1\":\"BRUNEI\",\"Value2\":\"BN\",\"Value3\":\"416\"},{\"Code\":\"BULGARIA\",\"Value1\":\"BULGARIA\",\"Value2\":\"BG\",\"Value3\":\"113\"},{\"Code\":\"BURKINA FASO\",\"Value1\":\"BURKINA FASO\",\"Value2\":\"BF\",\"Value3\":\"393\"},{\"Code\":\"BURUNDI\",\"Value1\":\"BURUNDI\",\"Value2\":\"BI\",\"Value3\":\"216\"},{\"Code\":\"KAMBODSJA\",\"Value1\":\"CAMBODIA\",\"Value2\":\"KH\",\"Value3\":\"478\"},{\"Code\":\"KAMERUN\",\"Value1\":\"CAMEROON\",\"Value2\":\"CM\",\"Value3\":\"270\"},{\"Code\":\"CANADA\",\"Value1\":\"CANADA\",\"Value2\":\"CA\",\"Value3\":\"612\"},{\"Code\":\"KAPP VERDE\",\"Value1\":\"CAPE VERDE\",\"Value2\":\"CV\",\"Value3\":\"273\"},{\"Code\":\"CAYMANØYENE\",\"Value1\":\"CAYMAN ISLANDS\",\"Value2\":\"KY\",\"Value3\":\"613\"},{\"Code\":\"SENTRALAFRIKANSKE REPUBLIKK\",\"Value1\":\"CENTRAL AFRICAN REPUBLIC\",\"Value2\":\"CF\",\"Value3\":\"337\"},{\"Code\":\"TCHAD\",\"Value1\":\"CHAD\",\"Value2\":\"TD\",\"Value3\":\"373\"},{\"Code\":\"CHILE\",\"Value1\":\"CHILE\",\"Value2\":\"CL\",\"Value3\":\"725\"},{\"Code\":\"KINA\",\"Value1\":\"CHINA\",\"Value2\":\"CN\",\"Value3\":\"484\"},{\"Code\":\"CHRISTMASØYA\",\"Value1\":\"CHRISTMAS ISLAND\",\"Value2\":\"CX\",\"Value3\":\"807\"},{\"Code\":\"KOKOSØYENE\",\"Value1\":\"COCOS ISLANDS (KEELING)\",\"Value2\":\"CC\",\"Value3\":\"808\"},{\"Code\":\"COLOMBIA\",\"Value1\":\"COLOMBIA\",\"Value2\":\"CO\",\"Value3\":\"730\"},{\"Code\":\"KOMORENE\",\"Value1\":\"COMOROS\",\"Value2\":\"KM\",\"Value3\":\"220\"},{\"Code\":\"KONGO, REPUBLIKKEN\",\"Value1\":\"CONGO\",\"Value2\":\"CG\",\"Value3\":\"278\"},{\"Code\":\"KONGO, DEN DEMOKR. REPUBL\",\"Value1\":\"CONGO, THE DEMOCRATIC REPUBLIC OF THE\",\"Value2\":\"CD\",\"Value3\":\"279\"},{\"Code\":\"COOKØYENE\",\"Value1\":\"COOK ISLANDS\",\"Value2\":\"CK\",\"Value3\":\"809\"},{\"Code\":\"COSTA RICA\",\"Value1\":\"COSTA RICA\",\"Value2\":\"CR\",\"Value3\":\"616\"},{\"Code\":\"ELFENBEINSKYSTEN\",\"Value1\":\"CÔTE D IVOIRE\",\"Value2\":\"CI\",\"Value3\":\"239\"},{\"Code\":\"KROATIA\",\"Value1\":\"CROATIA\",\"Value2\":\"HR\",\"Value3\":\"122\"},{\"Code\":\"CUBA\",\"Value1\":\"CUBA\",\"Value2\":\"CU\",\"Value3\":\"620\"},{\"Code\":\"CURACAO\",\"Value1\":\"CURACAO\",\"Value2\":\"CW\",\"Value3\":\"661\"},{\"Code\":\"KYPROS\",\"Value1\":\"CYPRUS\",\"Value2\":\"CY\",\"Value3\":\"500\"},{\"Code\":\"DEN TSJEKKISKE REP.\",\"Value1\":\"CZECHIA\",\"Value2\":\"CZ\",\"Value3\":\"158\"},{\"Code\":\"DANMARK\",\"Value1\":\"DENMARK\",\"Value2\":\"DK\",\"Value3\":\"101\"},{\"Code\":\"DJIBOUTI\",\"Value1\":\"DJIBOUTI\",\"Value2\":\"DJ\",\"Value3\":\"250\"},{\"Code\":\"DOMINICA\",\"Value1\":\"DOMINICA\",\"Value2\":\"DM\",\"Value3\":\"622\"},{\"Code\":\"DEN DOMINIKANSKE REP\",\"Value1\":\"DOMINICAN REPUBLIC\",\"Value2\":\"DO\",\"Value3\":\"624\"},{\"Code\":\"ECUADOR\",\"Value1\":\"ECUADOR\",\"Value2\":\"EC\",\"Value3\":\"735\"},{\"Code\":\"EGYPT\",\"Value1\":\"EGYPT\",\"Value2\":\"EG\",\"Value3\":\"249\"},{\"Code\":\"EL SALVADOR\",\"Value1\":\"EL SALVADOR\",\"Value2\":\"SV\",\"Value3\":\"672\"},{\"Code\":\"EKVATORIAL-GUINEA\",\"Value1\":\"EQUATORIAL GUINEA\",\"Value2\":\"GQ\",\"Value3\":\"235\"},{\"Code\":\"ERITREA\",\"Value1\":\"ERITREA\",\"Value2\":\"ER\",\"Value3\":\"241\"},{\"Code\":\"ESTLAND\",\"Value1\":\"ESTONIA\",\"Value2\":\"EE\",\"Value3\":\"115\"},{\"Code\":\"ETIOPIA\",\"Value1\":\"ETHIOPIA\",\"Value2\":\"ET\",\"Value3\":\"246\"},{\"Code\":\"FALKLANDSØYENE\",\"Value1\":\"FALKLAND ISLANDS\",\"Value2\":\"FK\",\"Value3\":\"740\"},{\"Code\":\"FÆRØYENE\",\"Value1\":\"FAROE ISLANDS\",\"Value2\":\"FO\",\"Value3\":\"104\"},{\"Code\":\"FIJII\",\"Value1\":\"FIJI\",\"Value2\":\"FJ\",\"Value3\":\"811\"},{\"Code\":\"FINLAND\",\"Value1\":\"FINLAND\",\"Value2\":\"FI\",\"Value3\":\"103\"},{\"Code\":\"FRANKRIKE\",\"Value1\":\"FRANCE\",\"Value2\":\"FR\",\"Value3\":\"117\"},{\"Code\":\"FRANSK GUYANA\",\"Value1\":\"FRENCH GUIANA\",\"Value2\":\"GF\",\"Value3\":\"745\"},{\"Code\":\"FRANSK POLYNESIA\",\"Value1\":\"FRENCH POLYNESIA\",\"Value2\":\"PF\",\"Value3\":\"814\"},{\"Code\":\"DE FRANSKE TERRITORIENE I SØR\",\"Value1\":\"FRENCH SOUTHERN TEERITORIES\",\"Value2\":\"TF\",\"Value3\":\"913\"},{\"Code\":\"GABON\",\"Value1\":\"GABON\",\"Value2\":\"GA\",\"Value3\":\"254\"},{\"Code\":\"GAMBIA\",\"Value1\":\"GAMBIA\",\"Value2\":\"GM\",\"Value3\":\"256\"},{\"Code\":\"GEORGIA\",\"Value1\":\"GEORGIA\",\"Value2\":\"GE\",\"Value3\":\"430\"},{\"Code\":\"TYSKLAND\",\"Value1\":\"GERMANY\",\"Value2\":\"DE\",\"Value3\":\"144\"},{\"Code\":\"GHANA\",\"Value1\":\"GHANA\",\"Value2\":\"GH\",\"Value3\":\"260\"},{\"Code\":\"GIBRALTAR\",\"Value1\":\"GIBRALTAR\",\"Value2\":\"GI\",\"Value3\":\"118\"},{\"Code\":\"STORBRITANNIA\",\"Value1\":\"GREAT BRITAIN\",\"Value2\":\"GB\",\"Value3\":\"139\"},{\"Code\":\"HELLAS\",\"Value1\":\"GREECE\",\"Value2\":\"GR\",\"Value3\":\"119\"},{\"Code\":\"GRØNLAND\",\"Value1\":\"GREENLAND\",\"Value2\":\"GL\",\"Value3\":\"102\"},{\"Code\":\"GRENADA\",\"Value1\":\"GRENADA\",\"Value2\":\"GD\",\"Value3\":\"629\"},{\"Code\":\"GUADELOUPE\",\"Value1\":\"GUADELOUPE\",\"Value2\":\"GP\",\"Value3\":\"631\"},{\"Code\":\"GUAM\",\"Value1\":\"GUAM\",\"Value2\":\"GU\",\"Value3\":\"817\"},{\"Code\":\"GUATEMALA\",\"Value1\":\"GUATEMALA\",\"Value2\":\"GT\",\"Value3\":\"632\"},{\"Code\":\"GUERNSEY\",\"Value1\":\"GUERNSEY\",\"Value2\":\"GG\",\"Value3\":\"162\"},{\"Code\":\"GUINEA\",\"Value1\":\"GUINEA\",\"Value2\":\"GN\",\"Value3\":\"264\"},{\"Code\":\"GUINEA-BISSAU\",\"Value1\":\"GUINEA-BISSAU\",\"Value2\":\"GW\",\"Value3\":\"266\"},{\"Code\":\"GUYANA\",\"Value1\":\"GUYANA\",\"Value2\":\"GY\",\"Value3\":\"720\"},{\"Code\":\"HAITI\",\"Value1\":\"HAITI\",\"Value2\":\"HT\",\"Value3\":\"636\"},{\"Code\":\"HEARD- OG MCDONALDØYENE\",\"Value1\":\"HEARD ISLAND  AND MCDONALD ISLANDS\",\"Value2\":\"HM\",\"Value3\":\"908\"},{\"Code\":\"HONDURAS\",\"Value1\":\"HONDURAS\",\"Value2\":\"HN\",\"Value3\":\"644\"},{\"Code\":\"HONGKONG\",\"Value1\":\"HONG KONG\",\"Value2\":\"HK\",\"Value3\":\"436\"},{\"Code\":\"UNGARN\",\"Value1\":\"HUNGARY\",\"Value2\":\"HU\",\"Value3\":\"152\"},{\"Code\":\"ISLAND\",\"Value1\":\"ICELAND\",\"Value2\":\"IS\",\"Value3\":\"105\"},{\"Code\":\"INDIA\",\"Value1\":\"INDIA\",\"Value2\":\"IN\",\"Value3\":\"444\"},{\"Code\":\"INDONESIA\",\"Value1\":\"INDONESIA\",\"Value2\":\"ID\",\"Value3\":\"448\"},{\"Code\":\"IRAN\",\"Value1\":\"IRAN\",\"Value2\":\"IR\",\"Value3\":\"456\"},{\"Code\":\"IRAK\",\"Value1\":\"IRAQ\",\"Value2\":\"IQ\",\"Value3\":\"452\"},{\"Code\":\"IRLAND\",\"Value1\":\"IRELAND\",\"Value2\":\"IE\",\"Value3\":\"121\"},{\"Code\":\"MAN\",\"Value1\":\"ISLE OF MAN\",\"Value2\":\"IM\",\"Value3\":\"164\"},{\"Code\":\"ISRAEL\",\"Value1\":\"ISRAEL\",\"Value2\":\"IL\",\"Value3\":\"460\"},{\"Code\":\"ITALIA\",\"Value1\":\"ITALY\",\"Value2\":\"IT\",\"Value3\":\"123\"},{\"Code\":\"JAMAICA\",\"Value1\":\"JAMAICA\",\"Value2\":\"JM\",\"Value3\":\"648\"},{\"Code\":\"JAPAN\",\"Value1\":\"JAPAN\",\"Value2\":\"JP\",\"Value3\":\"464\"},{\"Code\":\"JERSEY\",\"Value1\":\"JERSEY\",\"Value2\":\"JE\",\"Value3\":\"163\"},{\"Code\":\"JORDAN\",\"Value1\":\"JORDAN\",\"Value2\":\"JO\",\"Value3\":\"476\"},{\"Code\":\"KAZAKHSTAN\",\"Value1\":\"KAZAKHSTAN\",\"Value2\":\"KZ\",\"Value3\":\"480\"},{\"Code\":\"KENYA\",\"Value1\":\"KENYA\",\"Value2\":\"KE\",\"Value3\":\"276\"},{\"Code\":\"KIRIBATI\",\"Value1\":\"KIRIBATI\",\"Value2\":\"KI\",\"Value3\":\"815\"},{\"Code\":\"KOSOVO\",\"Value1\":\"KOSOVO\",\"Value2\":\"XK\",\"Value3\":\"161\"},{\"Code\":\"KUWAIT\",\"Value1\":\"KUWAIT\",\"Value2\":\"KW\",\"Value3\":\"496\"},{\"Code\":\"KYRGYZSTAN\",\"Value1\":\"KYRGYZSTAN\",\"Value2\":\"KG\",\"Value3\":\"502\"},{\"Code\":\"LAOS\",\"Value1\":\"LAOS\",\"Value2\":\"LA\",\"Value3\":\"504\"},{\"Code\":\"LATVIA\",\"Value1\":\"LATVIA\",\"Value2\":\"LV\",\"Value3\":\"124\"},{\"Code\":\"LIBANON\",\"Value1\":\"LEBANON\",\"Value2\":\"LB\",\"Value3\":\"508\"},{\"Code\":\"LESOTHO\",\"Value1\":\"LESOTHO\",\"Value2\":\"LS\",\"Value3\":\"281\"},{\"Code\":\"LIBERIA\",\"Value1\":\"LIBERIA\",\"Value2\":\"LR\",\"Value3\":\"283\"},{\"Code\":\"LIBYA\",\"Value1\":\"LIBYA\",\"Value2\":\"LY\",\"Value3\":\"286\"},{\"Code\":\"LIECHTENSTEIN\",\"Value1\":\"LIECHTENSTEIN\",\"Value2\":\"LI\",\"Value3\":\"128\"},{\"Code\":\"LITAUEN\",\"Value1\":\"LITHUANIA\",\"Value2\":\"LT\",\"Value3\":\"136\"},{\"Code\":\"LUXEMBOURG\",\"Value1\":\"LUXEMBOURG\",\"Value2\":\"LU\",\"Value3\":\"129\"},{\"Code\":\"MACAO\",\"Value1\":\"MACAU\",\"Value2\":\"MO\",\"Value3\":\"510\"},{\"Code\":\"MADAGASKAR\",\"Value1\":\"MADAGASCAR\",\"Value2\":\"MG\",\"Value3\":\"289\"},{\"Code\":\"MALAWI\",\"Value1\":\"MALAWI\",\"Value2\":\"MW\",\"Value3\":\"296\"},{\"Code\":\"MALAYSIA\",\"Value1\":\"MALAYSIA\",\"Value2\":\"MY\",\"Value3\":\"512\"},{\"Code\":\"MALDIVENE\",\"Value1\":\"MALDIVES\",\"Value2\":\"MV\",\"Value3\":\"513\"},{\"Code\":\"MALI\",\"Value1\":\"MALI\",\"Value2\":\"ML\",\"Value3\":\"299\"},{\"Code\":\"MALTA\",\"Value1\":\"MALTA\",\"Value2\":\"MT\",\"Value3\":\"126\"},{\"Code\":\"MARSHALLØYENE\",\"Value1\":\"MARSHALL ISLANDS\",\"Value2\":\"MH\",\"Value3\":\"835\"},{\"Code\":\"MARTINIQUE\",\"Value1\":\"MARTINIQUE\",\"Value2\":\"MQ\",\"Value3\":\"650\"},{\"Code\":\"MAURITANIA\",\"Value1\":\"MAURITANIA\",\"Value2\":\"MR\",\"Value3\":\"306\"},{\"Code\":\"MAURITIUS\",\"Value1\":\"MAURITIUS\",\"Value2\":\"MU\",\"Value3\":\"307\"},{\"Code\":\"MAYOTTE\",\"Value1\":\"MAYOTTE\",\"Value2\":\"YT\",\"Value3\":\"322\"},{\"Code\":\"MEXICO\",\"Value1\":\"MEXICO\",\"Value2\":\"MX\",\"Value3\":\"652\"},{\"Code\":\"MIKRONESIAFØDERASJONEN\",\"Value1\":\"MICRONESIA, FEDERATED STATES OF\",\"Value2\":\"FM\",\"Value3\":\"826\"},{\"Code\":\"DIVERSE\",\"Value1\":\"MISCELLANEOUS\",\"Value2\":\"ZZ\",\"Value3\":\"990\"},{\"Code\":\"MOLDOVA\",\"Value1\":\"MOLDOVA\",\"Value2\":\"MD\",\"Value3\":\"138\"},{\"Code\":\"MONACO\",\"Value1\":\"MONACO\",\"Value2\":\"MC\",\"Value3\":\"130\"},{\"Code\":\"MONGOLIA\",\"Value1\":\"MONGOLIA\",\"Value2\":\"MN\",\"Value3\":\"516\"},{\"Code\":\"MONTENEGRO\",\"Value1\":\"MONTENEGRO\",\"Value2\":\"ME\",\"Value3\":\"160\"},{\"Code\":\"MONTSERRAT\",\"Value1\":\"MONTSERRAT\",\"Value2\":\"MS\",\"Value3\":\"654\"},{\"Code\":\"MAROKKO\",\"Value1\":\"MOROCCO\",\"Value2\":\"MA\",\"Value3\":\"303\"},{\"Code\":\"MOSAMBIK\",\"Value1\":\"MOZAMBIQUE\",\"Value2\":\"MZ\",\"Value3\":\"319\"},{\"Code\":\"MYANMAR (BURMA)\",\"Value1\":\"MYANMAR\",\"Value2\":\"MM\",\"Value3\":\"420\"},{\"Code\":\"NAMIBIA\",\"Value1\":\"NAMIBIA\",\"Value2\":\"NA\",\"Value3\":\"308\"},{\"Code\":\"NAURU\",\"Value1\":\"NAURU\",\"Value2\":\"NR\",\"Value3\":\"818\"},{\"Code\":\"NEPAL\",\"Value1\":\"NEPAL\",\"Value2\":\"NP\",\"Value3\":\"528\"},{\"Code\":\"NEDERLAND\",\"Value1\":\"NETHERLANDS\",\"Value2\":\"NL\",\"Value3\":\"127\"},{\"Code\":\"DE NEDERLANDSKE ANTILLENE\",\"Value1\":\"NETHERLANDS ANTILLES\",\"Value2\":\"AN\",\"Value3\":\"656\"},{\"Code\":\"NY-KALEDONIA\",\"Value1\":\"NEW CALEDONIA\",\"Value2\":\"NC\",\"Value3\":\"833\"},{\"Code\":\"NEW ZEALAND\",\"Value1\":\"NEW ZEALAND\",\"Value2\":\"NZ\",\"Value3\":\"820\"},{\"Code\":\"NICARAGUA\",\"Value1\":\"NICARAGUA\",\"Value2\":\"NI\",\"Value3\":\"664\"},{\"Code\":\"NIGER\",\"Value1\":\"NIGER\",\"Value2\":\"NE\",\"Value3\":\"309\"},{\"Code\":\"NIGERIA\",\"Value1\":\"NIGERIA\",\"Value2\":\"NG\",\"Value3\":\"313\"},{\"Code\":\"NIUE\",\"Value1\":\"NIUE\",\"Value2\":\"NU\",\"Value3\":\"821\"},{\"Code\":\"NORFOLKØYA\",\"Value1\":\"NORFOLK ISLAND\",\"Value2\":\"NF\",\"Value3\":\"822\"},{\"Code\":\"NORD-KOREA\",\"Value1\":\"NORTH KOREA\",\"Value2\":\"KP\",\"Value3\":\"488\"},{\"Code\":\"NORD-MAKEDONIA\",\"Value1\":\"NORTH MACEDONIA\",\"Value2\":\"MK\",\"Value3\":\"156\"},{\"Code\":\"NORD-MARIANENE\",\"Value1\":\"NORTHERN MARIANA ISLANDS\",\"Value2\":\"MP\",\"Value3\":\"840\"},{\"Code\":\"NORGE\",\"Value1\":\"NORWAY\",\"Value2\":\"NO\",\"Value3\":\"000\"},{\"Code\":\"OMAN\",\"Value1\":\"OMAN\",\"Value2\":\"OM\",\"Value3\":\"520\"},{\"Code\":\"PAKISTAN\",\"Value1\":\"PAKISTAN\",\"Value2\":\"PK\",\"Value3\":\"534\"},{\"Code\":\"PALAU\",\"Value1\":\"PALAU\",\"Value2\":\"PW\",\"Value3\":\"839\"},{\"Code\":\"DET PALESTINSKE OMRÅDET\",\"Value1\":\"PALESTINE\",\"Value2\":\"PS\",\"Value3\":\"524\"},{\"Code\":\"PANAMA\",\"Value1\":\"PANAMA\",\"Value2\":\"PA\",\"Value3\":\"668\"},{\"Code\":\"PAPUA NY-GUINEA\",\"Value1\":\"PAPUA NEW GUINEA\",\"Value2\":\"PG\",\"Value3\":\"827\"},{\"Code\":\"PARAGUAY\",\"Value1\":\"PARAGUAY\",\"Value2\":\"PY\",\"Value3\":\"755\"},{\"Code\":\"PERU\",\"Value1\":\"PERU\",\"Value2\":\"PE\",\"Value3\":\"760\"},{\"Code\":\"FILIPPINENE\",\"Value1\":\"PHILIPPINES\",\"Value2\":\"PH\",\"Value3\":\"428\"},{\"Code\":\"PITCAIRN\",\"Value1\":\"PITCAIRN\",\"Value2\":\"PN\",\"Value3\":\"828\"},{\"Code\":\"POLEN\",\"Value1\":\"POLAND\",\"Value2\":\"PL\",\"Value3\":\"131\"},{\"Code\":\"PORTUGAL\",\"Value1\":\"PORTUGAL\",\"Value2\":\"PT\",\"Value3\":\"132\"},{\"Code\":\"PUERTO RICO\",\"Value1\":\"PUERTO RICO\",\"Value2\":\"PR\",\"Value3\":\"685\"},{\"Code\":\"QATAR\",\"Value1\":\"QATAR\",\"Value2\":\"QA\",\"Value3\":\"540\"},{\"Code\":\"RÉUNION\",\"Value1\":\"REUNION\",\"Value2\":\"RE\",\"Value3\":\"323\"},{\"Code\":\"ROMANIA\",\"Value1\":\"ROMANIA\",\"Value2\":\"RO\",\"Value3\":\"133\"},{\"Code\":\"RUSSLAND\",\"Value1\":\"RUSSIA\",\"Value2\":\"RU\",\"Value3\":\"140\"},{\"Code\":\"RWANDA\",\"Value1\":\"RWANDA\",\"Value2\":\"RW\",\"Value3\":\"329\"},{\"Code\":\"ST.HELENA\",\"Value1\":\"SAINT HELENA\",\"Value2\":\"SH\",\"Value3\":\"209\"},{\"Code\":\"ST.KITTS OG NEVIS\",\"Value1\":\"SAINT KITTS AND NEVIS\",\"Value2\":\"KN\",\"Value3\":\"677\"},{\"Code\":\"ST. LUCIA\",\"Value1\":\"SAINT LUCIA\",\"Value2\":\"LC\",\"Value3\":\"678\"},{\"Code\":\"SAINT MARTIN\",\"Value1\":\"SAINT MARTIN (FRENCH PART)\",\"Value2\":\"MF\",\"Value3\":\"686\"},{\"Code\":\"ST.PIERRE OG MIQUELON\",\"Value1\":\"SAINT PIERRE AND MIQUELON\",\"Value2\":\"PM\",\"Value3\":\"676\"},{\"Code\":\"ST. VINCENT OG GRENADINENE\",\"Value1\":\"SAINT VINCENT AND THE GRENADINES\",\"Value2\":\"VC\",\"Value3\":\"679\"},{\"Code\":\"VEST-SAMOA\",\"Value1\":\"SAMOA\",\"Value2\":\"WS\",\"Value3\":\"830\"},{\"Code\":\"SAN MARINO\",\"Value1\":\"SAN MARINO\",\"Value2\":\"SM\",\"Value3\":\"134\"},{\"Code\":\"SAO TOME OG PRINCIPE\",\"Value1\":\"SAO TOME AND PRINCIPE\",\"Value2\":\"ST\",\"Value3\":\"333\"},{\"Code\":\"SAUDI-ARABIA\",\"Value1\":\"SAUDI ARABIA\",\"Value2\":\"SA\",\"Value3\":\"544\"},{\"Code\":\"SENEGAL\",\"Value1\":\"SENEGAL\",\"Value2\":\"SN\",\"Value3\":\"336\"},{\"Code\":\"SERBIA\",\"Value1\":\"SERBIA\",\"Value2\":\"RS\",\"Value3\":\"159\"},{\"Code\":\"SEYCHELLENE\",\"Value1\":\"SEYCHELLES\",\"Value2\":\"SC\",\"Value3\":\"338\"},{\"Code\":\"SIERRA LEONE\",\"Value1\":\"SIERRA LEONE\",\"Value2\":\"SL\",\"Value3\":\"339\"},{\"Code\":\"SINGAPORE\",\"Value1\":\"SINGAPORE\",\"Value2\":\"SG\",\"Value3\":\"548\"},{\"Code\":\"SINT MAARTEN\",\"Value1\":\"SINT MAARTEN (DUTCH PART)\",\"Value2\":\"SX\",\"Value3\":\"658\"},{\"Code\":\"SLOVAKIA\",\"Value1\":\"SLOVAKIA\",\"Value2\":\"SK\",\"Value3\":\"157\"},{\"Code\":\"SLOVENIA\",\"Value1\":\"SLOVENIA\",\"Value2\":\"SI\",\"Value3\":\"146\"},{\"Code\":\"SALOMONØYENE\",\"Value1\":\"SOLOMON ISLANDS\",\"Value2\":\"SB\",\"Value3\":\"806\"},{\"Code\":\"SOMALIA\",\"Value1\":\"SOMALIA\",\"Value2\":\"SO\",\"Value3\":\"346\"},{\"Code\":\"SØR-AFRIKA\",\"Value1\":\"SOUTH AFRICA\",\"Value2\":\"ZA\",\"Value3\":\"359\"},{\"Code\":\"SØR-GEORGIA OG DE SØNDRE SANDWICHØYENE\",\"Value1\":\"SOUTH GEORGIA AND THE SOUTH SANDWICH ISLANDS\",\"Value2\":\"GS\",\"Value3\":\"907\"},{\"Code\":\"SØR-KOREA\",\"Value1\":\"SOUTH KOREA\",\"Value2\":\"KR\",\"Value3\":\"492\"},{\"Code\":\"SØR-SUDAN\",\"Value1\":\"SOUTH SUDAN\",\"Value2\":\"SS\",\"Value3\":\"355\"},{\"Code\":\"SPANIA\",\"Value1\":\"SPAIN\",\"Value2\":\"ES\",\"Value3\":\"137\"},{\"Code\":\"SRI LANKA\",\"Value1\":\"SRI LANKA\",\"Value2\":\"LK\",\"Value3\":\"424\"},{\"Code\":\"ST BARTHELEMY\",\"Value1\":\"ST BARTHELEMY\",\"Value2\":\"BL\",\"Value3\":\"687\"},{\"Code\":\"SUDAN\",\"Value1\":\"SUDAN\",\"Value2\":\"SD\",\"Value3\":\"356\"},{\"Code\":\"SURINAME\",\"Value1\":\"SURINAME\",\"Value2\":\"SR\",\"Value3\":\"765\"},{\"Code\":\"SVALBARD OG JAN MAYEN\",\"Value1\":\"SVALBARD AND JAN MAYEN\",\"Value2\":\"SJ\",\"Value3\":\"911\"},{\"Code\":\"SWAZILAND\",\"Value1\":\"SWAZILAND\",\"Value2\":\"SZ\",\"Value3\":\"357\"},{\"Code\":\"SVERIGE\",\"Value1\":\"SWEDEN\",\"Value2\":\"SE\",\"Value3\":\"106\"},{\"Code\":\"SVEITS\",\"Value1\":\"SWITZERLAND\",\"Value2\":\"CH\",\"Value3\":\"141\"},{\"Code\":\"SYRIA\",\"Value1\":\"SYRIA\",\"Value2\":\"SY\",\"Value3\":\"564\"},{\"Code\":\"TAIWAN\",\"Value1\":\"TAIWAN\",\"Value2\":\"TW\",\"Value3\":\"432\"},{\"Code\":\"TADZJIKISTAN\",\"Value1\":\"TAJIKISTAN\",\"Value2\":\"TJ\",\"Value3\":\"550\"},{\"Code\":\"TANZANIA\",\"Value1\":\"TANZANIA\",\"Value2\":\"TZ\",\"Value3\":\"369\"},{\"Code\":\"THAILAND\",\"Value1\":\"THAILAND\",\"Value2\":\"TH\",\"Value3\":\"568\"},{\"Code\":\"ØST-TIMOR\",\"Value1\":\"TIMOR-LESTE (EAST TIMOR)\",\"Value2\":\"TL\",\"Value3\":\"537\"},{\"Code\":\"TOGO\",\"Value1\":\"TOGO\",\"Value2\":\"TG\",\"Value3\":\"376\"},{\"Code\":\"TOKELAU\",\"Value1\":\"TOKELAU\",\"Value2\":\"TK\",\"Value3\":\"829\"},{\"Code\":\"TONGA\",\"Value1\":\"TONGA\",\"Value2\":\"TO\",\"Value3\":\"813\"},{\"Code\":\"TRINIDAD OG TOBAGO\",\"Value1\":\"TRINIDAD AND TOBAGO\",\"Value2\":\"TT\",\"Value3\":\"680\"},{\"Code\":\"TUNISIA\",\"Value1\":\"TUNISIA\",\"Value2\":\"TN\",\"Value3\":\"379\"},{\"Code\":\"TYRKIA\",\"Value1\":\"TURKEY\",\"Value2\":\"TR\",\"Value3\":\"143\"},{\"Code\":\"TURKMENISTAN\",\"Value1\":\"TURKMENISTAN\",\"Value2\":\"TM\",\"Value3\":\"552\"},{\"Code\":\"TURKS/CAICOSØYENE\",\"Value1\":\"TURKS AND CAICOS ISLANDS\",\"Value2\":\"TC\",\"Value3\":\"681\"},{\"Code\":\"TUVALU\",\"Value1\":\"TUVALU\",\"Value2\":\"TV\",\"Value3\":\"816\"},{\"Code\":\"UGANDA\",\"Value1\":\"UGANDA\",\"Value2\":\"UG\",\"Value3\":\"386\"},{\"Code\":\"UKRAINA\",\"Value1\":\"UKRAINE\",\"Value2\":\"UA\",\"Value3\":\"148\"},{\"Code\":\"DE ARABISKE EMIRATER\",\"Value1\":\"UNITED ARAB EMIRATES\",\"Value2\":\"AE\",\"Value3\":\"426\"},{\"Code\":\"USA\",\"Value1\":\"UNITED STATES\",\"Value2\":\"US\",\"Value3\":\"684\"},{\"Code\":\"MINDRE STILLEHAVSØYER\",\"Value1\":\"UNITED STATES MINOR OUTLYING ISLANDS\",\"Value2\":\"UM\",\"Value3\":\"819\"},{\"Code\":\"URUGUAY\",\"Value1\":\"URUGUAY\",\"Value2\":\"UY\",\"Value3\":\"770\"},{\"Code\":\"UZBEKISTAN\",\"Value1\":\"UZBEKISTAN\",\"Value2\":\"UZ\",\"Value3\":\"554\"},{\"Code\":\"VANUATU\",\"Value1\":\"VANUATU\",\"Value2\":\"VU\",\"Value3\":\"812\"},{\"Code\":\"VATIKANSTATEN\",\"Value1\":\"VATICAN (OR HOLY SEE)\",\"Value2\":\"VA\",\"Value3\":\"154\"},{\"Code\":\"VENEZUELA\",\"Value1\":\"VENEZUELA\",\"Value2\":\"VE\",\"Value3\":\"775\"},{\"Code\":\"VIETNAM\",\"Value1\":\"VIETNAM\",\"Value2\":\"VN\",\"Value3\":\"575\"},{\"Code\":\"JOMFRUØYENE BRIT.\",\"Value1\":\"VIRGIN ISLANDS, BRITISH\",\"Value2\":\"VG\",\"Value3\":\"608\"},{\"Code\":\"JOMFRUØYENE AM.\",\"Value1\":\"VIRGIN ISLANDS, U.S.\",\"Value2\":\"VI\",\"Value3\":\"601\"},{\"Code\":\"WALLIS/FUTUNAØYENE\",\"Value1\":\"WALLIS AND FUTUNA\",\"Value2\":\"WF\",\"Value3\":\"832\"},{\"Code\":\"VEST-SAHARA\",\"Value1\":\"WESTERN SAHARA\",\"Value2\":\"EH\",\"Value3\":\"304\"},{\"Code\":\"JEMEN\",\"Value1\":\"YEMEN\",\"Value2\":\"YE\",\"Value3\":\"578\"},{\"Code\":\"ZAMBIA\",\"Value1\":\"ZAMBIA\",\"Value2\":\"ZM\",\"Value3\":\"389\"},{\"Code\":\"ZIMBABWE\",\"Value1\":\"ZIMBABWE\",\"Value2\":\"ZW\",\"Value3\":\"326\"},{\"Code\":\"ÅLAND\",\"Value1\":\"ÅLAND ISLANDS\",\"Value2\":\"AX\",\"Value3\":\"902\"}],\"_links\":{\"self\":{\"href\":\"https://www.altinn.no/api/metadata/codelists/ASF_Land/2758?language=1033\"}}}",
            "https://www.altinn.no/api/metadata/codelists/ASF_land/2758?language=1044"
                => "{\"Name\": \"ASF_Land\",\"Version\": 2758,\"Language\": 1044,\"Codes\": [{ \"Code\": \"\", \"Value1\": \"\", \"Value2\": \"\", \"Value3\": \"\" },{\"Code\": \"MONTENEGRO\",\"Value1\": \"MONTENEGRO\",\"Value2\": \"ME\",\"Value3\": \"160\"},{ \"Code\": \"NORGE\", \"Value1\": \"NORGE\", \"Value2\": \"NO\", \"Value3\": \"000\" },{\"Code\": \"NY-KALEDONIA\",\"Value1\": \"NY-KALEDONIA\",\"Value2\": \"NC\",\"Value3\": \"833\"},{ \"Code\": \"OMAN\", \"Value1\": \"OMAN\", \"Value2\": \"OM\", \"Value3\": \"520\" },{\"Code\": \"PAKISTAN\",\"Value1\": \"PAKISTAN\",\"Value2\": \"PK\",\"Value3\": \"534\"},{ \"Code\": \"PALAU\", \"Value1\": \"PALAU\", \"Value2\": \"PW\", \"Value3\": \"839\" },{ \"Code\": \"PANAMA\", \"Value1\": \"PANAMA\", \"Value2\": \"PA\", \"Value3\": \"668\" },{\"Code\": \"PAPUA NY-GUINEA\",\"Value1\": \"PAPUA NY-GUINEA\",\"Value2\": \"PG\",\"Value3\": \"827\"},{\"Code\": \"PARAGUAY\",\"Value1\": \"PARAGUAY\",\"Value2\": \"PY\",\"Value3\": \"755\"},{ \"Code\": \"PERU\", \"Value1\": \"PERU\", \"Value2\": \"PE\", \"Value3\": \"760\" },{ \"Code\": \"POLEN\", \"Value1\": \"POLEN\", \"Value2\": \"PL\", \"Value3\": \"131\" },{\"Code\": \"PORTUGAL\",\"Value1\": \"PORTUGAL\",\"Value2\": \"PT\",\"Value3\": \"132\"},{\"Code\": \"SLOVAKIA\",\"Value1\": \"SLOVAKIA\",\"Value2\": \"SK\",\"Value3\": \"157\"},{\"Code\": \"SLOVENIA\",\"Value1\": \"SLOVENIA\",\"Value2\": \"SI\",\"Value3\": \"146\"},{ \"Code\": \"VIETNAM\", \"Value1\": \"VIETNAM\", \"Value2\": \"VN\", \"Value3\": \"575\" },{\"Code\": \"WALLIS/FUTUNAØYENE\",\"Value1\": \"WALLIS/FUTUNAØYENE\",\"Value2\": \"WF\",\"Value3\": \"832\"},{ \"Code\": \"ZAMBIA\", \"Value1\": \"ZAMBIA\", \"Value2\": \"ZM\", \"Value3\": \"389\" },{\"Code\": \"ZIMBABWE\",\"Value1\": \"ZIMBABWE\",\"Value2\": \"ZW\",\"Value3\": \"326\"},{\"Code\": \"ØSTERRIKE\",\"Value1\": \"ØSTERRIKE\",\"Value2\": \"AT\",\"Value3\": \"153\"},],\"_links\": {\"self\": {\"href\": \"https://www.altinn.no/api/metadata/codelists/ASF_Land/2758?language=1044\"}}}",
            "https://www.altinn.no/api/metadata/codelists/ASF_land/?language=1044"
                => "{\"Name\": \"ASF_Land\",\"Version\": 2758,\"Language\": 1044,\"Codes\": [{ \"Code\": \"\", \"Value1\": \"\", \"Value2\": \"\", \"Value3\": \"\" },{\"Code\": \"MONTENEGRO\",\"Value1\": \"MONTENEGRO\",\"Value2\": \"ME\",\"Value3\": \"160\"},{ \"Code\": \"NORGE\", \"Value1\": \"NORGE\", \"Value2\": \"NO\", \"Value3\": \"000\" },{\"Code\": \"NY-KALEDONIA\",\"Value1\": \"NY-KALEDONIA\",\"Value2\": \"NC\",\"Value3\": \"833\"},{ \"Code\": \"OMAN\", \"Value1\": \"OMAN\", \"Value2\": \"OM\", \"Value3\": \"520\" },{\"Code\": \"PAKISTAN\",\"Value1\": \"PAKISTAN\",\"Value2\": \"PK\",\"Value3\": \"534\"},{ \"Code\": \"PALAU\", \"Value1\": \"PALAU\", \"Value2\": \"PW\", \"Value3\": \"839\" },{ \"Code\": \"PANAMA\", \"Value1\": \"PANAMA\", \"Value2\": \"PA\", \"Value3\": \"668\" },{\"Code\": \"PAPUA NY-GUINEA\",\"Value1\": \"PAPUA NY-GUINEA\",\"Value2\": \"PG\",\"Value3\": \"827\"},{\"Code\": \"PARAGUAY\",\"Value1\": \"PARAGUAY\",\"Value2\": \"PY\",\"Value3\": \"755\"},{ \"Code\": \"PERU\", \"Value1\": \"PERU\", \"Value2\": \"PE\", \"Value3\": \"760\" },{ \"Code\": \"POLEN\", \"Value1\": \"POLEN\", \"Value2\": \"PL\", \"Value3\": \"131\" },{\"Code\": \"PORTUGAL\",\"Value1\": \"PORTUGAL\",\"Value2\": \"PT\",\"Value3\": \"132\"},{\"Code\": \"SLOVAKIA\",\"Value1\": \"SLOVAKIA\",\"Value2\": \"SK\",\"Value3\": \"157\"},{\"Code\": \"SLOVENIA\",\"Value1\": \"SLOVENIA\",\"Value2\": \"SI\",\"Value3\": \"146\"},{ \"Code\": \"VIETNAM\", \"Value1\": \"VIETNAM\", \"Value2\": \"VN\", \"Value3\": \"575\" },{\"Code\": \"WALLIS/FUTUNAØYENE\",\"Value1\": \"WALLIS/FUTUNAØYENE\",\"Value2\": \"WF\",\"Value3\": \"832\"},{ \"Code\": \"ZAMBIA\", \"Value1\": \"ZAMBIA\", \"Value2\": \"ZM\", \"Value3\": \"389\" },{\"Code\": \"ZIMBABWE\",\"Value1\": \"ZIMBABWE\",\"Value2\": \"ZW\",\"Value3\": \"326\"},{\"Code\": \"ØSTERRIKE\",\"Value1\": \"ØSTERRIKE\",\"Value2\": \"AT\",\"Value3\": \"153\"},],\"_links\": {\"self\": {\"href\": \"https://www.altinn.no/api/metadata/codelists/ASF_Land/2758?language=1044\"}}}",
            "https://www.altinn.no/api/metadata/codelists/ASF_Fylker/3063?language=1044"
                => "{\"Name\":\"ASF_Fylker\",\"Version\":3063,\"Language\":1044,\"Codes\":[{\"Code\":\"\",\"Value1\":\"\",\"Value2\":\"\",\"Value3\":\"\"},{\"Code\":\"Agder\",\"Value1\":\"Agder\",\"Value2\":\"4200\",\"Value3\":\"\"},{\"Code\":\"Akershus - UTGÅTT\",\"Value1\":\"Akershus - UTGÅTT\",\"Value2\":\"0200\",\"Value3\":\"\"},{\"Code\":\"Aust-Agder - UTGÅTT\",\"Value1\":\"Aust-Agder - UTGÅTT\",\"Value2\":\"0900\",\"Value3\":\"\"},{\"Code\":\"Buskerud - UTGÅTT\",\"Value1\":\"Buskerud -UTGÅTT\",\"Value2\":\"0600\",\"Value3\":\"\"},{\"Code\":\"Finnmark - UTGÅTT\",\"Value1\":\"Finnmark - UTGÅTT\",\"Value2\":\"2000\",\"Value3\":\"\"},{\"Code\":\"Hedmark - UTGÅTT\",\"Value1\":\"Hedmark - UTGÅTT\",\"Value2\":\"0400\",\"Value3\":\"\"},{\"Code\":\"Hordaland - UTGÅTT\",\"Value1\":\"Hordaland - UTGÅTT\",\"Value2\":\"1200\",\"Value3\":\"\"},{\"Code\":\"Innlandet\",\"Value1\":\"Innlandet\",\"Value2\":\"3400\",\"Value3\":\"\"},{\"Code\":\"Møre og Romsdal\",\"Value1\":\"Møre og Romsdal\",\"Value2\":\"1500\",\"Value3\":\"\"},{\"Code\":\"Nordland\",\"Value1\":\"Nordland\",\"Value2\":\"1800\",\"Value3\":\"\"},{\"Code\":\"Oppland - UTGÅTT\",\"Value1\":\"Oppland -UTGÅTT\",\"Value2\":\"0500\",\"Value3\":\"\"},{\"Code\":\"Oslo\",\"Value1\":\"Oslo\",\"Value2\":\"0300\",\"Value3\":\"\"},{\"Code\":\"Rogaland\",\"Value1\":\"Rogaland\",\"Value2\":\"1100\",\"Value3\":\"\"},{\"Code\":\"Sogn og Fjordane - UTGÅTT\",\"Value1\":\"Sogn og Fjordane - UTGÅTT\",\"Value2\":\"1400\",\"Value3\":\"\"},{\"Code\":\"Telemark - UTGÅTT\",\"Value1\":\"Telemark - UTGÅTT\",\"Value2\":\"0800\",\"Value3\":\"\"},{\"Code\":\"Troms - UTGÅTT\",\"Value1\":\"Troms - UTGÅTT\",\"Value2\":\"1900\",\"Value3\":\"\"},{\"Code\":\"Troms og Finnmark\",\"Value1\":\"Troms og Finnmark\",\"Value2\":\"5400\",\"Value3\":\"\"},{\"Code\":\"Trøndelag\",\"Value1\":\"Trøndelag\",\"Value2\":\"5000\",\"Value3\":\"\"},{\"Code\":\"Vest-Agder - UTGÅTT\",\"Value1\":\"Vest-Agder - UTGÅTT\",\"Value2\":\"1000\",\"Value3\":\"\"},{\"Code\":\"Vestfold og Telemark\",\"Value1\":\"Vestfold og Telemark\",\"Value2\":\"3800\",\"Value3\":\"\"},{\"Code\":\"Vestfold - UTGÅTT\",\"Value1\":\"Vestfold -UTGÅTT\",\"Value2\":\"0700\",\"Value3\":\"\"},{\"Code\":\"Vestland\",\"Value1\":\"Vestland\",\"Value2\":\"4600\",\"Value3\":\"\"},{\"Code\":\"Viken\",\"Value1\":\"Viken\",\"Value2\":\"3000\",\"Value3\":\"\"},{\"Code\":\"Østfold - UTGÅTT\",\"Value1\":\"Østfold - UTGÅTT\",\"Value2\":\"0100\",\"Value3\":\"\"}],\"_links\":{\"self\":{\"href\":\"https://www.altinn.no/api/metadata/codelists/ASF_Fylker/3063?language=1044\"}}}",
            "https://www.altinn.no/api/metadata/codelists/ASF_Fylker/3063?language=2068"
                => "{\"Name\":\"ASF_Fylker\",\"Version\":3063,\"Language\":1044,\"Codes\":[{\"Code\":\"\",\"Value1\":\"\",\"Value2\":\"\",\"Value3\":\"\"},{\"Code\":\"Agder\",\"Value1\":\"Agder\",\"Value2\":\"4200\",\"Value3\":\"\"},{\"Code\":\"Akershus - UTGÅTT\",\"Value1\":\"Akershus - UTGÅTT\",\"Value2\":\"0200\",\"Value3\":\"\"},{\"Code\":\"Aust-Agder - UTGÅTT\",\"Value1\":\"Aust-Agder - UTGÅTT\",\"Value2\":\"0900\",\"Value3\":\"\"},{\"Code\":\"Buskerud - UTGÅTT\",\"Value1\":\"Buskerud -UTGÅTT\",\"Value2\":\"0600\",\"Value3\":\"\"},{\"Code\":\"Finnmark - UTGÅTT\",\"Value1\":\"Finnmark - UTGÅTT\",\"Value2\":\"2000\",\"Value3\":\"\"},{\"Code\":\"Hedmark - UTGÅTT\",\"Value1\":\"Hedmark - UTGÅTT\",\"Value2\":\"0400\",\"Value3\":\"\"},{\"Code\":\"Hordaland - UTGÅTT\",\"Value1\":\"Hordaland - UTGÅTT\",\"Value2\":\"1200\",\"Value3\":\"\"},{\"Code\":\"Innlandet\",\"Value1\":\"Innlandet\",\"Value2\":\"3400\",\"Value3\":\"\"},{\"Code\":\"Møre og Romsdal\",\"Value1\":\"Møre og Romsdal\",\"Value2\":\"1500\",\"Value3\":\"\"},{\"Code\":\"Nordland\",\"Value1\":\"Nordland\",\"Value2\":\"1800\",\"Value3\":\"\"},{\"Code\":\"Oppland - UTGÅTT\",\"Value1\":\"Oppland -UTGÅTT\",\"Value2\":\"0500\",\"Value3\":\"\"},{\"Code\":\"Oslo\",\"Value1\":\"Oslo\",\"Value2\":\"0300\",\"Value3\":\"\"},{\"Code\":\"Rogaland\",\"Value1\":\"Rogaland\",\"Value2\":\"1100\",\"Value3\":\"\"},{\"Code\":\"Sogn og Fjordane - UTGÅTT\",\"Value1\":\"Sogn og Fjordane - UTGÅTT\",\"Value2\":\"1400\",\"Value3\":\"\"},{\"Code\":\"Telemark - UTGÅTT\",\"Value1\":\"Telemark - UTGÅTT\",\"Value2\":\"0800\",\"Value3\":\"\"},{\"Code\":\"Troms - UTGÅTT\",\"Value1\":\"Troms - UTGÅTT\",\"Value2\":\"1900\",\"Value3\":\"\"},{\"Code\":\"Troms og Finnmark\",\"Value1\":\"Troms og Finnmark\",\"Value2\":\"5400\",\"Value3\":\"\"},{\"Code\":\"Trøndelag\",\"Value1\":\"Trøndelag\",\"Value2\":\"5000\",\"Value3\":\"\"},{\"Code\":\"Vest-Agder - UTGÅTT\",\"Value1\":\"Vest-Agder - UTGÅTT\",\"Value2\":\"1000\",\"Value3\":\"\"},{\"Code\":\"Vestfold og Telemark\",\"Value1\":\"Vestfold og Telemark\",\"Value2\":\"3800\",\"Value3\":\"\"},{\"Code\":\"Vestfold - UTGÅTT\",\"Value1\":\"Vestfold -UTGÅTT\",\"Value2\":\"0700\",\"Value3\":\"\"},{\"Code\":\"Vestland\",\"Value1\":\"Vestland\",\"Value2\":\"4600\",\"Value3\":\"\"},{\"Code\":\"Viken\",\"Value1\":\"Viken\",\"Value2\":\"3000\",\"Value3\":\"\"},{\"Code\":\"Østfold - UTGÅTT\",\"Value1\":\"Østfold - UTGÅTT\",\"Value2\":\"0100\",\"Value3\":\"\"}],\"_links\":{\"self\":{\"href\":\"https://www.altinn.no/api/metadata/codelists/ASF_Fylker/3063?language=1044\"}}}",
            _ => null,
        };
    }
}
