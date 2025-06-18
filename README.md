FHIR date search handels timezone, percision up to minutes, for MVP i decided to not support seconds hence milisecodns range percision.<br>
https://drive.google.com/file/d/1NB9T5awDSK_Bygole8jcD9NfIBupZlCc/view<br><br>

yyyy-mm-ddThh:mm:ss.ssss[Z|(+|-)hh:mm]<br><br>

unicode + sign in query: %2B<br>
supports "eq", "ne", "gt", "lt", "ge", "le", "sa", "eb", "ap"<br><br>

**How to run**:<br>
There are 2 ways to generate SSL certificate:<br>
1) go to web-api root folder<br>
run PS from admin then run<br>
dotnet dev-certs https -ep "$env:APPDATA\ASP.NET\Https\Web.Api.pfx" -p "your_password"
then run<br>
dotnet user-secrets set "Kestrel:Certificates:Development:Password" "your_password"<br>
in .env file you can change your_password if needed
2) Go to VS and run web-api with Docker lauch profile, VS will create a cert, agree to what VS asks.<br>
Then delete created container by VS in docker. Also ports 5000 and 5001 might be not available on you machine, so release them.<br>

Then go to root folder and run docker-compose up -d --build<br>
to rerun(seed more) fhir-seeder-cli - just start a container again - it will seed 100 patients<br><br>

swagger<br>
https://localhost:5001/swagger/index.html<br>
http://localhost:5000/swagger/index.html<br>

**Some examples regarding precisions**:<br>


year precision will get anything within a year- eq2004<br>
month precision will get anything within a month eq2004-07<br>
day precision will get anything within a day - eq-2004-07-05 day<br>
hour precision will get anything within an hour - eq2004-07-05T09<br>
hour precision will get anything within minute - eq2004-07-05T09L39 minute<br>

**POST**<br>
1920  ->
"birthDate": "1920-01-01T00:00:00+00:00",
"birthDateOffset": null,
"originalBirthDate": "1920"
1920-01 ->
"birthDate": "1920-01-01T00:00:00+00:00",
"birthDateOffset": null,
"originalBirthDate": "1920-01",
1920-01-01 ->
"birthDate": "1920-01-01T00:00:00+00:00",
"birthDateOffset": null,
"originalBirthDate": "1920-01-01",
...
1920-01-01T10
1920-01-01T10:10
1920-01-01T10:10:10

1920-01-01T10+03:00->
"birthDate": "1920-01-01T07:00:00+00:00",
"birthDateOffset": "03:00:00",
"originalBirthDate": "1920-01-01T10+03:00",
1920-01-01T10:10:10Z ->
"birthDate": "1920-01-01T10:10:10+00:00",
"birthDateOffset": "00:00:00",
"originalBirthDate": "1920-01-01T10:10:10Z",
1920-01-01T10:10:10+00:00 ->
"birthDate": "1920-01-01T10:10:10+00:00",
"birthDateOffset": "00:00:00",
"originalBirthDate": "1920-01-01T10:10:10+00:00",

**GET**<br>
**eq**<br>
/patients?date=eq2004-07-05T09:39:02.372%2B05:00<br>
/patients?date=eq2004 year<br>
/patients?date=eq2004-07 month<br>
/patients?date=eq2004-07-05 day<br>
/patients?date=eq2004-07-05T09%2B05:00 hour<br>
/patients?date=eq2004-07-05T09:39%2B05:00 minutes<br>

**ge** **le**<br>
/patients?date=ge2004-07-05T09:39%2B05:00&date=le2004-07-05T09:39%2B05:00<br>
...
other operators

**Timezone adjsut example we get next day**<br>
POST /patients where birthDate 2004-07-05T10:40:05.373+14:00<br>
GET/patients/b95018d0-080a-4dc3-ac1a-cbc3bf1355c7<br>
"birthDate": "2004-07-04T20:40:05.373+00:00",<br>
"birthDateOffset": "14:00:00",<br>
"originalBirthDate": "2004-07-05T10:40:05.373+14:00",<br>
