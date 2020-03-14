/*
 * usart.c
 *
 * Created: 3/12/2020 10:10:54 PM
 *  Author: brana
 */ 

#include "main.h"

#define BAUD 9600
#define MYUBRR    F_CPU/16/BAUD-1

void uart_init(void)
{
	unsigned int uValue = MYUBRR;
	// setting the baud rate
	UBRR0H = (unsigned char)  ( uValue >> 8);
	UBRR0L = (unsigned char) uValue;
	
	// enabling TX & RX
	UCSR0B |= (1<<RXEN0)|(1<<TXEN0);
	UCSR0C |=  (1 << USBS0) | (3 << UCSZ00);    // Set frame: 8data, 2 stop

}

//Transmit a character
void usart_transmit( unsigned char data )
{
	/* Wait for empty transmit buffer */
	while (!( UCSR0A & (1<<UDRE0)));
	/* Put data into buffer, sends the data */
	UDR0 = data;
}

void usart_transmitString( char* data)
{
	while (!( UCSR0A & (1<<UDRE0)));
	
	while(*data > 0)
	{
		usart_transmit(*data++);
	}
}

//Receive a character
unsigned char usart_receive( void )
{
	/* Wait for data to be received */
	while (!(UCSR0A & (1<<RXC0)));
	/* Get and return received data from buffer */
	return UDR0;
}

void usart_receiveString(char* buffer, int bufferSize)
{
	while (!(UCSR0A & (1<<RXC0)));
	
	int i = 0;
	char dataChar;
		
	do 
	{
		dataChar = usart_receive();
		
		buffer[i++] = dataChar;
		
	} while ((i < bufferSize) && (dataChar != '\r'));
		
	 buffer[i] = '\0';
}