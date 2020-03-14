/*
 Author: Tyler Branan
 Date: 02/19/2019
 */

import java.util.Scanner;

public class JavaExample
{
	public static void main(String[] args) 
	{
		Scanner userInput = new Scanner(System.in);
		
		float baseTicketPrice = 100.0f;
		float finalTicketPrice = 0f;
		
		//A bool to determine if we qualified for a discount at the end.
		boolean qualifiedForDiscount = false;
		
		System.out.println("Welcome to Disney! To determine your eligible ticket discounts please enter the age of the visitor:");
		
		int userAge = userInput.nextInt();
		
		//Determine if the user is a senior, a child that gets free admission, or if the entered age is invalid.
		if(userAge >= 65)
		{
			//We qualified for a discount, so set this equal to true.
			qualifiedForDiscount = true;
			
			//Go ahead and apply the standard senior citizen discount, this may change later.
			finalTicketPrice = baseTicketPrice / 2f;
		}
		else if(userAge < 4 && userAge > 0)
		{
			//Children under four get in free, this means we can go ahead and end the program.
			finalTicketPrice = 0f;
			
			System.out.printf("\nThanks! Disney offers free ($%.2f) admission to all children under FOUR years old!\n", finalTicketPrice);
			
			//Call our system catch method to determine next action.
			systemCatch(args);
			userInput.close();

		}
		else if(userAge <= 0)
		{
			
			//The user has entered an invalid age, so they must restart.
			System.out.printf("\nUh-oh! You have entered an invalid age!\n");	
			
			//Call our system catch method to determine next action.
			systemCatch(args);
			userInput.close();
		}
		else
		{
			//If there are no discounts applied, the user will always receive the base price.
			finalTicketPrice = baseTicketPrice;
		}
		
		System.out.println("\nThanks! You may qualify for additional discounts. Please enter your state of residence.");
		
		String userResidence = userInput.next();
		
		//Determine discounts based on state residence. Set the string to be all uppercase for switch cases.
		switch(userResidence.toUpperCase())
		{
			//If the state residence is florida all tickets for those who are not senior citizens are $80
			case "FLORIDA":
				qualifiedForDiscount = true;
				finalTicketPrice = (userAge < 65) ? 80f : finalTicketPrice;
				break;
			
			//If the state residence is georgia all tickets for those who are under the age of 14 get an 18% discount
			case "GEORGIA":
				qualifiedForDiscount = true;
				finalTicketPrice = (userAge < 14) ? (baseTicketPrice - (baseTicketPrice * .18f)) : finalTicketPrice;
				break;
				
			//If the state residence is texas all tickets for those who are senior citizens get an additional 7.5% discount
			case "TEXAS":
				qualifiedForDiscount = true;
				finalTicketPrice = (userAge >= 65) ? (baseTicketPrice / 2 - (baseTicketPrice * .075f)) : finalTicketPrice;
				break;
		}

		
		if(qualifiedForDiscount)
		{
			System.out.printf("\nThanks! You qualified for an additional discounts! Your discounted ticket price will be $%.2f!\n", finalTicketPrice);
			
			//Call our system catch method to determine next action.
			systemCatch(args);
			userInput.close();
		}
		else
		{
			System.out.printf("\n\nThanks! Your final ticket price will be $%.2f!\n", finalTicketPrice);
			
			//Call our system catch method to determine next action.
			systemCatch(args);
			userInput.close();
		}
	}
	
	//A method for catching system errors or closing the system! Pass userInput and args to the method to recall the main() method.
	public static void systemCatch(String[] args)
	{	
		Scanner systemInput = new Scanner(System.in);
		
		System.out.println("\nPlease enter ''RERUN'' to restart the program, otherwise the program will close!");
		
		//A string value for entering in commands such as exit, rerun, etc.
		String command = systemInput.next();
		
		//Decide if we want to rerun the system or not.
		if(command.equalsIgnoreCase("rerun"))
		{
			System.out.println("\nSYSTEM: RESTARTING PROGRAM!\n");	
			
			//Restarts the program
			Assignment4.main(args);
			systemInput.close();
		}

		else
		{
			System.out.print("\nGoodbye!\n");	
			
			//Exits the program.
			System.exit(0);
			systemInput.close();
		}
	}
}