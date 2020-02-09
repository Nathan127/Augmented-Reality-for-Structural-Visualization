unsigned long time;
void setup() {
 Serial.begin(9600);
}
void loop() {
  float sensorValue = analogRead(A0);
  float sensorValue2 = analogRead(A1);
  float voltage = sensorValue * (5.0 / 1024.0);
  float voltate2 = sensorValue2* (5.0 / 1024.0);
  time = millis();
  Serial.print("7/1/2016;00:00:00.0;"); //prints time since program started
  Serial.print(voltage); //prints time since program started
  Serial.print(";"); //prints time since program started
  Serial.print(voltate2); //prints time since program started
  Serial.println();
  delay(50);   
}
