/*
 * C_DisplayTest.c
 *
 * Created: 3/8/2020 4:20:44 PM
 * Author : brana
 */ 

#include "main.h"

#define INPUTSIZE 20

char input[INPUTSIZE];

int main ()
{
	uart_init();

	while(1) 
	{
		
		usart_receiveString(input, INPUTSIZE);

		usart_transmitString("Full String: ");
		usart_transmitString(input[0]);
		
		_delay_ms(100);
	}
	
	return 0;
}