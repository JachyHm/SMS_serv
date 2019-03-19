#include <stdio.h>
#include <windows.h>

// Definuj promenne
HANDLE hSerial;
DCB dcbSerialParams = { 0 };
COMMTIMEOUTS timeouts = { 0 };

int initCOM(HANDLE *hSerial, char *comName)
{
	// Otevri nejvyssi dostupny COM
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
	dcbSerialParams.BaudRate = CBR_57600;
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

void sendMessage(HANDLE *hSerial, char *messageToSend)
{
	// Posli text
	DWORD bytes_written = 0;
	fprintf(stderr, "Posilam zpravu %s o delce %d byty...\n", messageToSend, strlen(messageToSend));

	if (!WriteFile(*hSerial, &messageToSend, strlen(messageToSend), &bytes_written, NULL))
	{
		fprintf(stderr, "Chyba!\n"); 
		short Errorcode = GetLastError();
		fprintf(stderr, "Error: %d\n", Errorcode);
		return 1;
	}
	else fprintf(stderr, "Zapsano %d bytu\n", bytes_written);

	Sleep(100);
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

void main()
{
	char com[5] = "COM1";
	if (!initCOM(&hSerial, &com)) {
		char zprava[50] = "AT+CMGF=1";
		sendMessage(&hSerial, &zprava);
		char zprava1[50] = "AT+CMGW=\"+420728346650\"";
		sendMessage(&hSerial, &zprava1);
		char zprava2[50] = "Achoj Vobo!";
		sendMessage(&hSerial, &zprava2);
		char zprava3[50] = "AT+CMSS=1";
		sendMessage(&hSerial, &zprava3);
		Sleep(1000);
		deinitCOM(&hSerial);
	}
	Sleep(50000);
}