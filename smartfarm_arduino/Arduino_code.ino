#include <DHT.h>

#define DHTPIN 11
#define DHTTYPE DHT11
DHT dht(DHTPIN, DHTTYPE);

void setup() {
  Serial.begin(9600);
  dht.begin();
}

void loop() {
  float t = dht.readTemperature();
  if (!isnan(t)) {
    Serial.println(t, 1);  // 예: 24.6
  }
  delay(1000);

}
