#include <stdio.h>
#include <windows.h>

// Definuj promenne
HANDLE hSerial;
DCB dcbSerialParams = { 0 };
COMMTIMEOUTS timeouts = { 0 };
char recBuffer[10];

void append(char *str, char c)
{
	for (; *str; str++);
	*str++ = c;
	*str++ = 0;
}

int initCOM(HANDLE *hSerial, char *comName)
{
	// Otevri zadany COM
	fprintf(stderr, "Oteviram seriovy port %s...", comName);
	*hSerial = CreateFile(
		comName, GENERIC_READ | GENERIC_WRITE, 0, NULL,
		OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (*hSerial == INVALID_HANDLE_VALUE)
	{
		fprintf(stderr, "Chyba\nNepodarilo se otevrit %s...\n", comName);
		return 1;
	}
	else fprintf(stderr, "OK\n");

	// Nastav parametry (57600 bd, 8 data bit, 1 start bit,
	// 1 stop bit, no parity)
	dcbSerialParams.DCBlength = sizeof(dcbSerialParams);
	if (GetCommState(*hSerial, &dcbSerialParams) == 0)
	{
		fprintf(stderr, "Nastala chyba pri dotazu na stav zarizeni\n");
		CloseHandle(*hSerial);
		return 1;
	}

	fprintf(stderr, "Nastavuji parametry zarizeni...");
	dcbSerialParams.BaudRate = CBR_115200;
	dcbSerialParams.ByteSize = 8;
	dcbSerialParams.StopBits = ONESTOPBIT;
	dcbSerialParams.Parity = NOPARITY;
	if (SetCommState(*hSerial, &dcbSerialParams) == 0)
	{
		fprintf(stderr, "Chyba\nNastala chyba pri pokusu o zapis nastaveni\n");
		CloseHandle(*hSerial);
		return 1;
	}
	else fprintf(stderr, "OK\n");

	// Nastav casy vyprseni zadosti
	timeouts.ReadIntervalTimeout = 50;
	timeouts.ReadTotalTimeoutConstant = 50;
	timeouts.ReadTotalTimeoutMultiplier = 10;
	timeouts.WriteTotalTimeoutConstant = 50;
	timeouts.WriteTotalTimeoutMultiplier = 10;
	if (SetCommTimeouts(*hSerial, &timeouts) == 0)
	{
		fprintf(stderr, "Chyba pri nastavovani timeoutu\n");
		CloseHandle(*hSerial);
		return 1;
	}

	// vrat OK
	return 0;
};

void sendCommand(HANDLE *hSerial, char *messageToSend, char *receivedMessage)
{
	// Vytvor DWORD bytes_written - pocet odeslanych bytr
	DWORD bytes_written = 0;

	//Vypis
	fprintf(stderr, "Posilam zpravu %s o delce %d byte...\n", messageToSend, strlen(messageToSend));

	//Pridej na konec zpravy CR
	append(messageToSend, '\r');

	//Posli zpravu o delce strlen
	if (!WriteFile(*hSerial, messageToSend, strlen(messageToSend), &bytes_written, NULL))
	{
		fprintf(stderr, "Chyba!\n"); 
		short Errorcode = GetLastError();
		fprintf(stderr, "Error: %d\n", Errorcode);
		return 1;
	}
	else fprintf(stderr, "Zapsano %d bytu\n", bytes_written);

	//Vytvor DWORD bytes_read - pocet prijatych byte
	DWORD bytes_read = 0;

	//Vypis
	fprintf(stderr, "Cekam na odpoved...");

	//Definuj int gotResponse - jestli uz byly prijate data
	int gotResponse = FALSE;

	while (TRUE)
	{
		//nuluj pocet prijatych byte
		bytes_read = 0;

		//precti 1 bajt
		if (ReadFile(*hSerial, recBuffer, 1, &bytes_read, NULL))
		{
			//pokud byla prijata nejaka data
			if (bytes_read > 0)
			{
				//a jsou prvni
				if (!gotResponse) {
					strcpy_s(receivedMessage, 1024, recBuffer);
				}
				//a uz byla prijata nejaka predtim
				else
				{
					strcat_s(receivedMessage, 1024, recBuffer);
				}
				//zapis, ze byla prectena data
				gotResponse = TRUE;
			}
			//pokud je prazdny buffer, ale prijali jsme data, ukonci cteni
			else if (gotResponse)
			{
				break;
			}
		}
		//chyba
		else
		{
			fprintf(stderr, "Chyba!\n");
			short Errorcode = GetLastError();
			fprintf(stderr, "Error: %d\n", Errorcode);
			return 1;
		}
	}

	fprintf(stderr, "OK\n");
	
	fprintf(stderr, "Precteno %d byte\n", strlen(receivedMessage));
}

void deinitCOM(HANDLE * hSerial)
{
	// Zavri sp
	fprintf(stderr, "Zaviram COM...");
	if (CloseHandle(*hSerial) == 0)
	{
		fprintf(stderr, "Chyba\n");
		return 1;
	}
	else fprintf(stderr, "OK\n");

	// vrat OK
	return 0;
}

void sendSMS(const char* telNum, const char* mess) 
{
	char recMess[1024];
	memset(recMess, 0, 1024);
	const char txtModAtCommand[20] = "AT+CMGF=1";
	sendCommand(&hSerial, txtModAtCommand, &recMess);
	char *ptr = strstr(recMess, "\n");
	fprintf(stderr, ptr);
	memset(recMess, 0, 1024);
	char *recipientAtCommand = malloc(100 * sizeof(char));
	strcpy_s(recipientAtCommand, 100, "AT+CMGS=\"");
	strcat_s(recipientAtCommand, 100, telNum);
	strcat_s(recipientAtCommand, 100, "\"");
	sendCommand(&hSerial, recipientAtCommand, &recMess);
	*ptr = strstr(recMess, "\n");
	fprintf(stderr, ptr);
	memset(recMess, 0, 1024);
	sendCommand(&hSerial, mess, &recMess);
	*ptr = strstr(recMess, "\n");
	fprintf(stderr, ptr);
}

void main()
{
	char com[5] = "COM9";
	if (!initCOM(&hSerial, &com)) {

		char number[50] = "00420725245545";
		char message[200] = "Ahoj Babi 2!\x1A";
		sendSMS(number, message);
		Sleep(1000);
		deinitCOM(&hSerial);
	}
	Sleep(50000);
}