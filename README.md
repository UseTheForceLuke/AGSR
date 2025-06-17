FHIR date search handels timezone, percision up to minutes, for MVP i decided to not support seconds and milisecodns percision.

yyyy-mm-ddThh:mm:ss.ssss[Z|(+|-)hh:mm]

unicode + sign in query: %2B

Some examples:

POST /patients
where "birthDate":
2004-07-05T09:39:02.372+05:00 year
2004-08-05T09:39:02.372+05:00

2004-07-05T10:39:02.372+05:00 month
2004-07-06T10:39:02.372+05:00

2004-07-05T09:39:02.372+05:00 day
2004-07-05T10:39:02.372+05:00

2004-07-05T09:39:02.372+05:00 hour
2004-07-05T09:40:02.372+05:00

2004-07-05T09:39:02.372+05:00 minute
2004-07-05T09:39:03.372+05:00

GET
eq
/patients?date=eq2004-07-05T09:39:02.372%2B05:00
/patients?date=eq2004 year
/patients?date=eq2004-07 month
/patients?date=eq2004-07-05 day
/patients?date=eq2004-07-05T09%2B05:00 hour
/patients?date=eq2004-07-05T09:39%2B05:00 minutes

/patients?date=ge2004-07-05T09:39%2B05:00&date=le2004-07-05T09:39%2B05:00
