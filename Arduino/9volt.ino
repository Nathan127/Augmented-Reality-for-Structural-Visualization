unsigned long time;
void setup() {
 Serial.begin(9600);
}
void loop() {
  float sensorValue = analogRead(A0);
  float voltage = sensorValue * (5.0 / 1024.0);
  time = millis();
  Serial.print("7/1/2016;00:00:00.0;"); //prints time since program started
  Serial.println(voltage);
  delay(50);   
}
